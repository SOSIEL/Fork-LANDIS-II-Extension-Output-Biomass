//  Authors:  Robert M. Scheller, James B. Domingo
//  Modified by:  SOSIEL Inc.

using System;
using System.Collections.Generic;
using System.Linq;

using Landis.Core;
using Landis.Library.Biomass;
using Landis.Library.BiomassCohorts;
using Landis.Library.HarvestManagement;
using Landis.Library.Metadata;
using Landis.SpatialModeling;

namespace Landis.Extension.Output.Biomass
{
    public class PlugIn : ExtensionMain
    {
        public static readonly ExtensionType ExtType = new ExtensionType("output");
        public static readonly string ExtensionName = "Output Biomass";

        public static IEnumerable<ISpecies> speciesToMap;
        public static string speciesTemplateToMap;
        public static string poolsToMap;
        public static string poolsTemplateToMap;
        private IEnumerable<ISpecies> selectedSpecies;
        private string speciesMapNameTemplate;
        private string selectedPools;
        private string poolMapNameTemplate;
        private IInputParameters parameters;
        private static ICore modelCore;
        private bool makeTableByEcoRegion;
        private bool makeTableByManagementArea;
        private List<ManagementArea> managementAreas;
        public static MetadataTable<SummaryLogByEcoRegion> summaryLogByEcoRegion;
        public static MetadataTable<SummaryLogByMamanementArea> summaryLogByManagementArea;

        //---------------------------------------------------------------------

        public PlugIn()
            : base(ExtensionName, ExtType)
        {
        }

        //---------------------------------------------------------------------

        public static ICore ModelCore
        {
            get
            {
                return modelCore;
            }
        }

        //---------------------------------------------------------------------

        public override void LoadParameters(string dataFile, ICore mCore)
        {
            modelCore = mCore;
            InputParametersParser parser = new InputParametersParser();
            parameters = Landis.Data.Load<IInputParameters>(dataFile, parser);
        }

        //---------------------------------------------------------------------

        public override void Initialize()
        {
            SiteVars.Initialize();
            makeTableByManagementArea = parameters.MakeTableByManagementArea;
            if (makeTableByManagementArea && SiteVars.ManagementArea == null)
            {
                throw new ApplicationException(
                    "Output by management area is requested, but management areas are not available." +
                    " Please make sure that you are using proper version of the BiomassHarvest extension" +
                    " and it is active."
                );
            }
            makeTableByEcoRegion = parameters.MakeTableByEcoRegion;
            Timestep = parameters.Timestep;
            selectedSpecies = parameters.SelectedSpecies;
            speciesToMap = selectedSpecies;
            speciesMapNameTemplate = parameters.SpeciesMapNames;
            speciesTemplateToMap = speciesMapNameTemplate;
            selectedPools = parameters.SelectedPools;
            poolsToMap = selectedPools;
            poolMapNameTemplate = parameters.PoolMapNames;
            poolsTemplateToMap = poolMapNameTemplate;
            string logByManagementArea = (SiteVars.ManagementArea == null) ? null : "spp-biomass-by-ma-log.csv";
            MetadataHandler.InitializeMetadata(
                parameters.Timestep, "spp-biomass-log.csv", logByManagementArea, makeTableByEcoRegion, makeTableByManagementArea);
        }

        //---------------------------------------------------------------------

        public override void Run()
        {
            WriteMapForAllSpecies();

            if (makeTableByEcoRegion)
                WriteLogFile();

            if (makeTableByManagementArea)
                WriteLogFileByManagementArea();

            WritePoolMaps();

            if (selectedSpecies != null)
                WriteSpeciesMaps();
        }

        //---------------------------------------------------------------------

        private void WriteSpeciesMaps()
        {
            foreach (ISpecies species in selectedSpecies)
            {
                string path = MakeSpeciesMapName(species.Name);
                ModelCore.UI.WriteLine("   Writing {0} biomass map to {1} ...", species.Name, path);
                using (IOutputRaster<IntPixel> outputRaster = modelCore.CreateRaster<IntPixel>(path, modelCore.Landscape.Dimensions))
                {
                    IntPixel pixel = outputRaster.BufferPixel;
                    foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                    {
                        if (site.IsActive)
                            pixel.MapCode.Value = (int)Math.Round((double)ComputeSpeciesBiomass(SiteVars.Cohorts[site][species]));
                        else
                            pixel.MapCode.Value = 0;
                        outputRaster.WriteBufferPixel();
                    }
                }
            }
        }

        //---------------------------------------------------------------------

        private void WriteMapForAllSpecies()
        {
            // Biomass map for all species
            string path = MakeSpeciesMapName("TotalBiomass");
            PlugIn.ModelCore.UI.WriteLine("   Writing total biomass map to {0} ...", path);
            using (IOutputRaster<IntPixel> outputRaster = modelCore.CreateRaster<IntPixel>(path, modelCore.Landscape.Dimensions))
            {
                IntPixel pixel = outputRaster.BufferPixel;
                foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                {
                    if (site.IsActive)
                        pixel.MapCode.Value = (int) Math.Round((double) ComputeTotalBiomass(SiteVars.Cohorts[site]));
                    else
                        pixel.MapCode.Value = 0;

                    outputRaster.WriteBufferPixel();
                }
            }
        }

        //---------------------------------------------------------------------

        private string MakeSpeciesMapName(string species)
        {
            return SpeciesMapNames.ReplaceTemplateVars(speciesMapNameTemplate,
                                                       species,
                                                       PlugIn.ModelCore.CurrentTime);
        }


        //---------------------------------------------------------------------

        private void WritePoolMaps()
        {
            if(selectedPools == "woody" || selectedPools == "both")
                WritePoolMap("woody", SiteVars.WoodyDebris);

            if(selectedPools == "non-woody" || selectedPools == "both")
                WritePoolMap("non-woody", SiteVars.Litter);
        }

        //---------------------------------------------------------------------

        private void WritePoolMap(string         poolName,
                                  ISiteVar<Pool> poolSiteVar)
        {
            string path = PoolMapNames.ReplaceTemplateVars(poolMapNameTemplate,
                                                           poolName,
                                                           PlugIn.ModelCore.CurrentTime);
            if(poolSiteVar != null)
            {
                PlugIn.ModelCore.UI.WriteLine("   Writing {0} biomass map to {1} ...", poolName, path);
                using (IOutputRaster<IntPixel> outputRaster = modelCore.CreateRaster<IntPixel>(path, modelCore.Landscape.Dimensions))
                {
                    IntPixel pixel = outputRaster.BufferPixel;
                    foreach (Site site in PlugIn.ModelCore.Landscape.AllSites)
                    {
                        if (site.IsActive)
                            pixel.MapCode.Value = (int)(float)poolSiteVar[site].Mass;
                        else
                            pixel.MapCode.Value = 0;
                        outputRaster.WriteBufferPixel();
                    }
                }
            }
        }

        //---------------------------------------------------------------------

        private void WriteLogFile()
        {
            double[,] allSppEcos = new double[ModelCore.Ecoregions.Count, ModelCore.Species.Count];
            int[] activeSiteCount = new int[ModelCore.Ecoregions.Count];

            //UI.WriteLine("Next, accumulate data.");

            foreach (ActiveSite site in ModelCore.Landscape)
            {
                IEcoregion ecoregion = ModelCore.Ecoregion[site];
                foreach (ISpecies species in ModelCore.Species)
                {
                    allSppEcos[ecoregion.Index, species.Index]
                        += ComputeSpeciesBiomass(SiteVars.Cohorts[site][species]);
                }
                activeSiteCount[ecoregion.Index]++;
            }

            foreach (IEcoregion ecoregion in ModelCore.Ecoregions)
            {
                summaryLogByEcoRegion.Clear();
                var sl = new SummaryLogByEcoRegion();
                sl.Time = modelCore.CurrentTime;
                sl.EcoName = ecoregion.Name;
                sl.NumActiveSites = activeSiteCount[ecoregion.Index];

                double[] aboveBiomass = new double[modelCore.Species.Count];
                foreach (ISpecies species in ModelCore.Species)
                {
                    aboveBiomass[species.Index] = activeSiteCount[ecoregion.Index] > 0
                        ? allSppEcos[ecoregion.Index, species.Index] / activeSiteCount[ecoregion.Index] 
                        : 0;
                }
                sl.AboveGroundBiomass_ = aboveBiomass;

                summaryLogByEcoRegion.AddObject(sl);
                summaryLogByEcoRegion.WriteToFile();
            }
        }

        //---------------------------------------------------------------------

        private static int ComputeSpeciesBiomass(ISpeciesCohorts cohorts)
        {
            int total = 0;
            if (cohorts != null)
            {
                foreach (ICohort cohort in cohorts)
                    total += cohort.Biomass;
            }
            return total;
        }

        //---------------------------------------------------------------------

        private static int ComputeTotalBiomass(ISiteCohorts cohorts)
        {
            int total = 0;
            if (cohorts != null)
            {
                foreach (ISpeciesCohorts speciesCohorts in cohorts)
                    total += ComputeSpeciesBiomass(speciesCohorts);
            }
            return total;
        }

        //---------------------------------------------------------------------

        private void WriteLogFileByManagementArea()
        {
            if (managementAreas == null)
            {
                managementAreas = CollectManagementAreas();
            }

            double[,] allSppManagementAreas = new double[managementAreas.Count, ModelCore.Species.Count];
            int[] activeSiteCount = new int[managementAreas.Count];

            //UI.WriteLine("Next, accumulate data.");

            for (int i = 0; i < managementAreas.Count; ++i)
            {
                var managementArea = managementAreas[i];
                foreach (var stand in managementArea)
                {
                    foreach (var site in stand)
                    {
                        foreach (ISpecies species in ModelCore.Species)
                        {
                            allSppManagementAreas[i, species.Index]
                                += ComputeSpeciesBiomass(SiteVars.Cohorts[site][species]);
                        }
                        activeSiteCount[i]++;
                    }
                }
            }

            for (int i = 0; i < managementAreas.Count; ++i)
            {
                var managementArea = managementAreas[i];
                summaryLogByManagementArea.Clear();
                var sl = new SummaryLogByMamanementArea();
                sl.Time = modelCore.CurrentTime;
                sl.ManagementAreaMapCode = (int)managementArea.MapCode;
                sl.NumActiveSites = activeSiteCount[i];

                double[] aboveBiomass = new double[modelCore.Species.Count];
                foreach (ISpecies species in ModelCore.Species)
                {
                    aboveBiomass[species.Index] = activeSiteCount[i] > 0
                        ? allSppManagementAreas[i, species.Index] / activeSiteCount[i]
                        : 0;
                }
                sl.AboveGroundBiomass_ = aboveBiomass;

                summaryLogByManagementArea.AddObject(sl);
                summaryLogByManagementArea.WriteToFile();
            }
        }

        //---------------------------------------------------------------------

        private static List<ManagementArea> CollectManagementAreas()
        {
            var managementAreas = new Dictionary<uint, ManagementArea>();
            foreach (var site in ModelCore.Landscape.ActiveSites)
            {
                var managementArea = SiteVars.ManagementArea[site];
                if (managementArea != null && !managementAreas.ContainsKey(managementArea.MapCode))
                    managementAreas.Add(managementArea.MapCode, managementArea);
            }
            return managementAreas.Values.OrderBy(ma => ma.MapCode).ToList();
        }
    }
}
