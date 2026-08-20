using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TruckLib.Models.Ppd;

namespace TruckLib.Tests.TruckLib.ScsMap
{
    public class PrefabFixture
    {
        public PrefabDescriptor BlkwCrossingPpd { get; } =
            PrefabDescriptor.Open("Data/PrefabTest/blkw_r1_x_r1_narrow_tmpl.ppd");

        public PrefabDescriptor FrTCrossingPpd { get; } =
            PrefabDescriptor.Open("Data/PrefabTest/fr_r1_x_r1_t_narrow_tmpl.ppd");

        public PrefabDescriptor CompanyPpd { get; } =
            PrefabDescriptor.Open("Data/PrefabTest/car_dealer_01_fr.ppd");

        public PrefabDescriptor ServicePpd { get; } =
            PrefabDescriptor.Open("Data/PrefabTest/gas_plaza_01_ger.ppd");

        public Models.Model ServicePmd { get; } =
            Models.Model.Open("Data/PrefabTest/gas_plaza_01_ger.pmd");
    }
}
