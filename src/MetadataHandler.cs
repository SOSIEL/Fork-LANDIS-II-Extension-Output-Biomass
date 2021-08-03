//  Modified by:  SOSIEL Inc.

using System;
using System.IO;

using Landis.Core;
using Landis.Library.Metadata;

namespace Landis.Extension.Output.Biomass
{
    public static class MetadataHandler
    {
        public static ExtensionMetadata Extension { get; set; }
        
        public static void InitializeMetadata(
            int Timestep, string summaryLogByEcoRegion, string summaryLogByManagementArea,
            bool makeTable, bool makeTableByManagementArea
        )
        {

            ScenarioReplicationMetadata scenRep = new ScenarioReplicationMetadata()
            {
                RasterOutCellArea = PlugIn.ModelCore.CellArea,
                TimeMin = PlugIn.ModelCore.StartTime,
                TimeMax = PlugIn.ModelCore.EndTime,
            };

            Extension = new ExtensionMetadata(PlugIn.ModelCore)
            {
                Name = PlugIn.ExtensionName,
                TimeInterval = Timestep, 
                ScenarioReplicationMetadata = scenRep
            };

            //---------------------------------------
            //          table outputs:
            //---------------------------------------

            if (makeTable)
            {
                CreateDirectory(summaryLogByEcoRegion);
                PlugIn.summaryLogByEcoRegion = new MetadataTable<SummaryLogByEcoRegion>(summaryLogByEcoRegion);
                PlugIn.ModelCore.UI.WriteLine("   Generating summary table by ecoregions...");
                OutputMetadata tblSummaryByEcoRegion = new OutputMetadata()
                {
                    Type = OutputType.Table,
                    Name = "SummaryLog",
                    FilePath = PlugIn.summaryLogByEcoRegion.FilePath,
                    Visualize = true,
                };
                tblSummaryByEcoRegion.RetriveFields(typeof(SummaryLogByEcoRegion));
                Extension.OutputMetadatas.Add(tblSummaryByEcoRegion);
            }

            if (makeTableByManagementArea)
            {
                CreateDirectory(summaryLogByManagementArea);
                PlugIn.summaryLogByManagementArea = new MetadataTable<SummaryLogByMamanementArea>(summaryLogByManagementArea);
                PlugIn.ModelCore.UI.WriteLine("   Generating summary table by management areas...");
                OutputMetadata tblSummaryByManagementArea = new OutputMetadata()
                {
                    Type = OutputType.Table,
                    Name = "SummaryLogByManagementArea",
                    FilePath = PlugIn.summaryLogByManagementArea.FilePath,
                    Visualize = true,
                };
                tblSummaryByManagementArea.RetriveFields(typeof(SummaryLogByMamanementArea));
                Extension.OutputMetadatas.Add(tblSummaryByManagementArea);
            }

            //2 kinds of maps: species and pool maps, maybe multiples of each?
            //---------------------------------------            
            //          map outputs:         
            //---------------------------------------


            foreach (ISpecies species in PlugIn.speciesToMap)
            {
                OutputMetadata mapOut_Species = new OutputMetadata()
                {
                    Type = OutputType.Map,
                    Name = species.Name,
                    FilePath = SpeciesMapNames.ReplaceTemplateVars(PlugIn.speciesTemplateToMap,
                                                       species.Name,
                                                       PlugIn.ModelCore.CurrentTime),
                    Map_DataType = MapDataType.Continuous,
                    Visualize = true,
                    //Map_Unit = "categorical",
                };
                Extension.OutputMetadatas.Add(mapOut_Species);
            }

            OutputMetadata mapOut_TotalBiomass = new OutputMetadata()
            {
                Type = OutputType.Map,
                Name = "TotalBiomass",
                FilePath = SpeciesMapNames.ReplaceTemplateVars(PlugIn.speciesTemplateToMap,
                                       "TotalBiomass",
                                       PlugIn.ModelCore.CurrentTime),
                Map_DataType = MapDataType.Continuous,
                Visualize = true,
                //Map_Unit = "categorical",
            };
            Extension.OutputMetadatas.Add(mapOut_TotalBiomass);

            if (PlugIn.poolsToMap == "both" || PlugIn.poolsToMap == "woody")
            {
                OutputMetadata mapOut_WoodyDebris = new OutputMetadata()
                {
                    Type = OutputType.Map,
                    Name = "WoodyDebrisMap",
                    FilePath = PoolMapNames.ReplaceTemplateVars(PlugIn.poolsTemplateToMap,
                                                           "woody",
                                                           PlugIn.ModelCore.CurrentTime),
                    Map_DataType = MapDataType.Continuous,
                    Visualize = true,
                    //Map_Unit = "categorical",
                };
                Extension.OutputMetadatas.Add(mapOut_WoodyDebris);
            }

            if (PlugIn.poolsToMap == "non-woody" || PlugIn.poolsToMap == "both")
            {
                OutputMetadata mapOut_NonWoodyDebris = new OutputMetadata()
                {
                    Type = OutputType.Map,
                    Name = "NonWoodyDebrisMap",
                    FilePath = PoolMapNames.ReplaceTemplateVars(PlugIn.poolsTemplateToMap,
                                           "non-woody",
                                           PlugIn.ModelCore.CurrentTime),
                    Map_DataType = MapDataType.Continuous,
                    Visualize = true,
                    //Map_Unit = "categorical",
                };
                Extension.OutputMetadatas.Add(mapOut_NonWoodyDebris);
            }

            //---------------------------------------
            MetadataProvider mp = new MetadataProvider(Extension);
            mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);
        }

        public static void CreateDirectory(string path)
        {
            path = path.Trim(null);
            if (path.Length == 0)
                throw new ArgumentException("path is empty or just whitespace");

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Landis.Utilities.Directory.EnsureExists(dir);
            }
        }
    }
}