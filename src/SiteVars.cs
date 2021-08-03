//  Authors:  Robert M. Scheller, James B. Domingo
//  Modified by:  SOSIEL Inc.

using Landis.Library.Biomass;
using Landis.Library.BiomassCohorts;
using Landis.Library.HarvestManagement;
using Landis.SpatialModeling;

namespace Landis.Extension.Output.Biomass
{
    /// <summary>
    /// The pools of dead biomass for the landscape's sites.
    /// </summary>
    public static class SiteVars
    {
        private static ISiteVar<Pool> woodyDebris;
        private static ISiteVar<Pool> litter;
        private static ISiteVar<ISiteCohorts> cohorts;
        private static ISiteVar<ManagementArea> managementArea;

        //---------------------------------------------------------------------

        /// <summary>
        /// Initializes the module.
        /// </summary>
        public static void Initialize()
        {

            woodyDebris = PlugIn.ModelCore.GetSiteVar<Pool>("Succession.WoodyDebris");
            litter = PlugIn.ModelCore.GetSiteVar<Pool>("Succession.Litter");

            cohorts = PlugIn.ModelCore.GetSiteVar<ISiteCohorts>("Succession.BiomassCohorts");
            if (cohorts == null)
            {
                string mesg = string.Format("Cohorts are empty.  Please double-check that this extension is compatible with your chosen succession extension.");
                throw new System.ApplicationException(mesg);
            }

            // Optional, may be not available
            managementArea = PlugIn.ModelCore.GetSiteVar<ManagementArea>("BiomassHarvest.ManagementArea");
        }

        //---------------------------------------------------------------------
        public static ISiteVar<ISiteCohorts> Cohorts
        {
            get
            {
                return cohorts;
            }
        }
        //---------------------------------------------------------------------

        /// <summary>
        /// The intact dead woody pools for the landscape's sites.
        /// </summary>
        public static ISiteVar<Pool> WoodyDebris
        {
            get
            {
                return woodyDebris;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// The dead non-woody pools for the landscape's sites.
        /// </summary>
        public static ISiteVar<Pool> Litter
        {
            get
            {
                return litter;
            }
        }

        //---------------------------------------------------------------------

        /// <summary>
        /// A management area which the site belong to.
        /// May be provided when SOSIEL Inc. modified version of the BiomassHarvest extension is used.
        /// </summary>
        public static ISiteVar<ManagementArea> ManagementArea
        {
            get
            {
                return managementArea;
            }
        }
    }
}
