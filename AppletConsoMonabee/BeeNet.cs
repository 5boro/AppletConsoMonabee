using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppletConsoMonabee
{
    class BeeNet
    {
        public string ip { get; set; }
        public string nom { get; set; }
        public string mac { get; set; }

        static List<BeeNet> lesBeeNets = new List<BeeNet>();

    	public BeeNet(string ipBeeNet, string nomBeeNet, string macBeeNet){
    		ip=ipBeeNet;
    		nom=nomBeeNet;
            mac = macBeeNet;
    	}

        public void ajouterBeeNet(){
            lesBeeNets.Add(this);
        }

        public static List<BeeNet> getlistBeeNets()
        {
            return lesBeeNets;
        }
    }
}
