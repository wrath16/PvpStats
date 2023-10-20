using Dalamud.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PvpStats.Types {
    internal class PlayerAlias {
        public string Name { get; set; } = "";
        public string HomeWorld { get; set; } = "";
        //public string FirstName => !Name.IsNullOrEmpty() ? Name.Split(' ')[0] : "";
        //public string LastName => !Name.IsNullOrEmpty() ? Name.Split(' ')[1] : "";


        public string FirstName {
            get {
                if(Name.IsNullOrEmpty()) {
                    return "";
                }
                string[] split = Name.Split(" ");
                if(split.Length > 0) {
                    return split[0];
                }
                return "";
            }
        }


        public string LastName {
            get {
                if(Name.IsNullOrEmpty()) {
                    return "";
                }
                string[] split = Name.Split(" ");
                if(split.Length > 1) {
                    return split[1];
                }
                return "";
            }
        }

        //public PlayerAlias() {
        //}
    }
}
