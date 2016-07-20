﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Landis.Library.Metadata;
using Edu.Wisc.Forest.Flel.Util;
using Landis.Core;
using System.IO;
using Flel = Edu.Wisc.Forest.Flel;

namespace Landis.Extension.Output.Biomass
{
    public class MetadataHandler
    {
        
        public static ExtensionMetadata Extension {get; set;}

        public static void InitializeMetadata(int Timestep, IEnumerable<ISpecies> selectedSpecies, string sppMapNames, ICore mCore)
        {
            ScenarioReplicationMetadata scenRep = new ScenarioReplicationMetadata() {
                RasterOutCellArea = PlugIn.ModelCore.CellArea,
                TimeMin = PlugIn.ModelCore.StartTime,
                TimeMax = PlugIn.ModelCore.EndTime
            };

            Extension = new ExtensionMetadata(mCore){
                Name = PlugIn.ExtensionName,
                TimeInterval = Timestep, 
                ScenarioReplicationMetadata = scenRep
            };

            //---------------------------------------
            //          table outputs:   
            //---------------------------------------

            string sppBiomassLog = ("output-biomass/spp-biomass-log.csv");
            CreateDirectory(sppBiomassLog);
            PlugIn.sppBiomassLog = new MetadataTable<SppBiomassLog>(sppBiomassLog);

            OutputMetadata tblOut_events = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = "SppBiomassLog",
                FilePath = PlugIn.sppBiomassLog.FilePath,
                Visualize = true
            };
            tblOut_events.RetriveFields(typeof(SppBiomassLog));
            Extension.OutputMetadatas.Add(tblOut_events);

            string sppBiomassLogLandscape = ("output-biomass/biomass-log-landscape.csv");
            CreateDirectory(sppBiomassLogLandscape);
            PlugIn.sppBiomassLogLandscape = new MetadataTable<SppBiomassLogLandscape>(sppBiomassLogLandscape);

            tblOut_events = new OutputMetadata()
            {
                Type = OutputType.Table,
                Name = ("BiomassLogLandscape"),
                FilePath = PlugIn.sppBiomassLogLandscape.FilePath,
                Visualize = true
            };
            tblOut_events.RetriveFields(typeof(SppBiomassLogLandscape));
            Extension.OutputMetadatas.Add(tblOut_events);

            //---------------------------------------            
            //          map outputs:         
            //---------------------------------------
            if (PlugIn.MakeMaps)
            {
                foreach (ISpecies species in selectedSpecies)
                {
                    string sppMapPath = SpeciesMapNames.ReplaceTemplateVars(sppMapNames, species.Name);

                    OutputMetadata mapOut_SppBiomass = new OutputMetadata()
                    {
                        Type = OutputType.Map,
                        Name = ("Species Biomass Map: " + species.Name),
                        FilePath = @sppMapPath,
                        Map_DataType = MapDataType.Continuous,
                        Map_Unit = FieldUnits.g_B_m2,
                        Visualize = true
                    };
                    Extension.OutputMetadatas.Add(mapOut_SppBiomass);
                }

            }

            string totalBioMapPath = SpeciesMapNames.ReplaceTemplateVars(sppMapNames, "TotalBiomass");

            OutputMetadata mapOut_TotalBiomass = new OutputMetadata()
            {
                Type = OutputType.Map,
                Name = ("Total Biomass Map"),
                FilePath = @totalBioMapPath,
                Map_DataType = MapDataType.Continuous,
                Map_Unit = FieldUnits.g_B_m2,
                Visualize = true
            };
            Extension.OutputMetadatas.Add(mapOut_TotalBiomass);
            
            //---------------------------------------
            MetadataProvider mp = new MetadataProvider(Extension);
            mp.WriteMetadataToXMLFile("Metadata", Extension.Name, Extension.Name);




        }

        public static void CreateDirectory(string path)
        {
            //Require.ArgumentNotNull(path);
            path = path.Trim(null);
            if (path.Length == 0)
                throw new ArgumentException("path is empty or just whitespace");

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Flel.Util.Directory.EnsureExists(dir);
            }

            //return new StreamWriter(path);
            return;
        }
    }

}
