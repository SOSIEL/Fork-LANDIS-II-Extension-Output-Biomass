//  Modified by:  SOSIEL Inc.

using Landis.Library.Metadata;

namespace Landis.Extension.Output.Biomass
{
    public class SummaryLogByEcoRegion
    {
        [DataField(Unit = FieldUnits.Year, Desc = "Simulation Year")]
        public int Time { set; get; }

        [DataField(Desc = "Ecoregion Name")]
        public string EcoName { set; get; }

        [DataField(Unit = FieldUnits.Count, Desc = "Number of Active Sites")]
        public int NumActiveSites { set; get; }

        [DataField(Desc = "Mean Aboveground Biomass by Species", SppList = true)]
        public double[] AboveGroundBiomass_ { set; get; }
    }

    public class SummaryLogByMamanementArea
    {
        [DataField(Unit = FieldUnits.Year, Desc = "Simulation Year")]
        public int Time { set; get; }

        [DataField(Desc = "Management Area Map Code")]
        public int ManagementAreaMapCode { set; get; }

        [DataField(Unit = FieldUnits.Count, Desc = "Number of Active Sites")]
        public int NumActiveSites { set; get; }

        [DataField(Desc = "Mean Aboveground Biomass by Species", SppList = true)]
        public double[] AboveGroundBiomass_ { set; get; }
    }
}
