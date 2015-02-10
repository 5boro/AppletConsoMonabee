using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AppletConsoMonabee
{
    public class Mesure
    {
        public string netBIOS { get; set; }
        public string macBeeNet { get; set; }
        public DateTime heure { get; set; }
        public int conso { get; set; }
        public int typechassis { get; set; }
        public string evenement { get; set; }
        public bool envoyé { get; set; }
        static List<Mesure> lesMesures = new List<Mesure>();

        public Mesure(string unNetBIOS, string uneMacBeeNet, DateTime uneHeure, int uneConso, int unTypeChassis, string unEvenement, bool estEnvoyé)
        {
            netBIOS = unNetBIOS;
            macBeeNet = uneMacBeeNet;
            heure = uneHeure;
            conso = uneConso;
            typechassis = unTypeChassis;
            evenement = unEvenement;
            envoyé = estEnvoyé;
        }

        public void ajouterMesure(){
            lesMesures.Add(this);
        }

        public static List<Mesure> getlistMesures()
        {
            return lesMesures;
        }
    }
}
