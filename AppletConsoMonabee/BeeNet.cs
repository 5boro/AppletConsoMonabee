using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppletConsoMonabee
{
    class BeeNet
    {
    	string ip;
    	string nom;
        static List<BeeNet> lesBeeNets = new List<BeeNet>();

    	public BeeNet(string ipBeeNet, string nomBeeNet){
    		ip=ipBeeNet;
    		nom=nomBeeNet;
    	}

        public void ajouterBeeNet(){
            lesBeeNets.Add(this);
        }

        public string getNom()
        {
            return nom;
        }

        public string getIp()
        {
            return ip;
        }

        public static List<BeeNet> getlistBeeNets()
        {
            return lesBeeNets;
        }
    }
}
