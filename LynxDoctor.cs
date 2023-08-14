using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LynxDoctor
{
    public class LynxVolumeDoctor
    {
        // Universal
        private double TipVolMax;
        private string InputTransferVolumeString { get; set; }
        private List<double> VolumeList { get; set; }

        // Specific to VOLUME PARSING AND REFACTORING:
        private int ParseCount { get; set; }
        private string OutputTransferVolumeString { get; set; }
        private bool NeedsAdjustment { get; set; }
        private List<int> VolumeFragmentsNeededList { get; set; }
        private List<double> BlowoutVolumeList { get; set; }
        private List<double> UpdatedVolumeList { get; set; }
        public List<int> SplitOccuranceList { get; set; }
        public int TransferCycleCount { get; set; }

        // Specific to MIXING STRING CREATION:
        private const double MIX_SCALING_FACTOR = 0.75;
        private const int UPPER_MIX_COUNT = 8;
        private const int LOWER_MIX_COUNT = 5;
        public int MixCount { get; set; }
        private List<double> MixVolumeList { get; set; }
        private string MixVolumeString { get; set; }

        // Specific to MAX VOLUME VALIDATION:
        private Dictionary<string, double> PlateDictionary { get; set; }
        private string PlateType { get; set; }


        // CONSTRUCTOR
        public LynxVolumeDoctor(string inputVolList, double tipVolMax, string plateType)
        {
            // Universal
            InputTransferVolumeString = inputVolList;
            TipVolMax = tipVolMax;
            VolumeList = FormatLynxVolume.ExtractVolList(inputVolList);
            PlateType = plateType;

            // Specific to VOLUME PARSING AND REFACTORING:
            ParseCount = 0;
            VolumeFragmentsNeededList = new List<int>();
            BlowoutVolumeList = new List<double>();
            UpdatedVolumeList = new List<double>();
            SplitOccuranceList = new List<int>();
            for (int i = 0; i < 96; i++)
                SplitOccuranceList.Add(0);

            // Specific to MIXING STRING CREATION:
            MixCount = 0;
            MixVolumeList = new List<double>();

            // Specific to MAX VOLUME VALIDATION:
            PlateDictionary = new Dictionary<string, double>();
            PlateType = plateType;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////// START: VOLUME PARSING AND REFACTORING /////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        public void CheckAdjustmentNeeds()
        {
            for (int i = 0; i < VolumeList.Count; i++)
            {
                if (Convert.ToDouble(VolumeList[i]) >= TipVolMax) { NeedsAdjustment = true; break; }
                else { NeedsAdjustment = false; }
            }
        }

        // Creates a list of need iteration of aspiration and dispensing
        // Max value of VolumeFragmentsNeededList will control the cycle count of trasnfer
        public void CreateFragmentsList()
        {
            for (int i = 0; i < VolumeList.Count; i++)
            {
                int volSplitCount_Temp = 0;
                double currentVol = Convert.ToDouble(VolumeList[i]);
                if (currentVol <= TipVolMax) { volSplitCount_Temp = 0; }
                else { volSplitCount_Temp = (int)(currentVol / TipVolMax); }
                if (volSplitCount_Temp == 1) { volSplitCount_Temp++; }
                VolumeFragmentsNeededList.Add(volSplitCount_Temp);
            }

            // Find max volume
            TransferCycleCount = VolumeFragmentsNeededList.Max();
        }

        // Creates updated volume list
        public void UpdateVolList()
        {
            UpdatedVolumeList.Clear();
            int maxSplitOccurance = SplitOccuranceList.Max();
            for (int i = 0; i < VolumeList.Count(); i++)
            {
                if (VolumeFragmentsNeededList[i] != SplitOccuranceList[i])
                {
                    double currnetSplitCount = Convert.ToDouble(VolumeFragmentsNeededList[i]);
                    if (currnetSplitCount == 1) { currnetSplitCount++; }
                    double newVol = Convert.ToInt32(VolumeList[i]) / currnetSplitCount;
                    UpdatedVolumeList.Add(newVol);
                    SplitOccuranceList[i] = SplitOccuranceList[i] + 1;
                }
                else if (maxSplitOccurance == 0) { UpdatedVolumeList.Add(Convert.ToDouble(VolumeList[i])); }
                else { UpdatedVolumeList.Add(0.0); }
            }
        }


        public String VolumeDoctorDriver()
        {
            Console.WriteLine("Strating...");
            CheckAdjustmentNeeds();
            if (NeedsAdjustment)
            {
                if (ParseCount == 0) { CreateFragmentsList(); } //Max value created here
                if (FormatLynxVolume.ContainsBlowout(InputTransferVolumeString))
                {
                    BlowoutVolumeList = FormatLynxVolume.ExtractBlowoutVolList(InputTransferVolumeString);
                    UpdateVolList();
                    OutputTransferVolumeString = FormatLynxVolume.ConvertListToVolString(InputTransferVolumeString, UpdatedVolumeList, BlowoutVolumeList, true);
                }
                else
                {
                    UpdateVolList();
                    OutputTransferVolumeString = FormatLynxVolume.ConvertListToVolString(InputTransferVolumeString, UpdatedVolumeList);

                }
                ParseCount++;
                return OutputTransferVolumeString;
            }
            else { return InputTransferVolumeString; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////// END: VOLUME PARSING AND REFACTORING ///////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////// START: MIXING STRING CREATION /////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void setMixingCycle()
        {
            double maxVol = VolumeList.Max();
            if (maxVol > TipVolMax) { MixCount = UPPER_MIX_COUNT; }
            else { MixCount = LOWER_MIX_COUNT; }
        }

        public void CreateMixVolList()
        {
            MixVolumeList.Clear();
            for (int i = 0; i < VolumeList.Count(); i++)
            {
                if (VolumeList[i] < TipVolMax)
                {
                    MixVolumeList.Add(VolumeList[i] * MIX_SCALING_FACTOR);
                }
                else { MixVolumeList.Add(TipVolMax); }
            }
        }


        public String MixingDoctorDriver()
        {
            setMixingCycle(); // call 'get MixCount' in main to control mix count
            CreateMixVolList();
            MixVolumeString = "";
            MixVolumeString = FormatLynxVolume.ConvertListToVolString(InputTransferVolumeString, MixVolumeList);
            return MixVolumeString;
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////// END: MIXING STRING CREATION ///////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////// START: MAX VOLUME VALIDATION //////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void addNewPlate(string plateName, double maxVolume)
        {
            if (!plateName.Equals("") && maxVolume > 0) { PlateDictionary.Add(plateName, maxVolume); }
        }

        public void removePlate(string plateName)
        {
            if (!plateName.Equals("")) { PlateDictionary.Remove(plateName); }
        }

        public bool ValidMaxVolDoctorDriver()
        {
            double maxVolumeInputString = Convert.ToDouble(VolumeList.Max());
            double maxVolume = PlateDictionary[PlateType];
            if (maxVolumeInputString <= maxVolume) { return true; }
            else { return false; }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //////////// END: MAX VOLUME VALIDATION ////////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    }

    public class FormatLynxVolume
    {
        // Check for blowout
        public static bool ContainsBlowout(string volumeString)
        {
            if (volumeString.Substring(9).Contains(';')) { return true; }
            else { return false; }
        }

        // Format to volume only list 
        public static List<double> ExtractVolList(string volumeString)
        {
            List<double> volumeList = new List<double>();
            string[] tempList = volumeString.Split(',');
            if (!ContainsBlowout(volumeString))
            {
                for (int i = 1; i < tempList.Length; i++)
                {
                    volumeList.Add(Convert.ToDouble(tempList[i]));
                }
            }
            else
            {
                for (int i = 1; i < tempList.Length; i++)
                {
                    string[] dispenseTempList = tempList[i].Split(';');
                    volumeList.Add(Convert.ToDouble(dispenseTempList[0]));
                }
            }
            return volumeList;
        }

        // Format blowout volume only list
        public static List<double> ExtractBlowoutVolList(string volumeString)
        {
            List<double> blowoutVolumeList = new List<double>();
            string[] tempList = volumeString.Split(',');
            if (ContainsBlowout(volumeString))
            {
                for (int i = 1; i < tempList.Length; i++)
                {
                    string[] dispenseTempList = tempList[i].Split(';');
                    blowoutVolumeList.Add(Convert.ToDouble(dispenseTempList[1]));
                }
            }
            else
            {
                return blowoutVolumeList;
            }
            return blowoutVolumeList;
        }

        public static string ConvertListToVolString(string orginalVolumeString, List<double> volList, List<double> blowoutVolList = null, bool containsBlowout = false)
        {
            string outputTransferVolumeString = "";
            outputTransferVolumeString = orginalVolumeString.Substring(0, 8);
            if (containsBlowout)
            {
                for (int i = 0; i < volList.Count(); i++)
                {
                    outputTransferVolumeString += Convert.ToString(volList[i]) + ";";
                    if (volList[i] > 0) { outputTransferVolumeString += blowoutVolList[i] + ","; }
                    else { outputTransferVolumeString += "0.0,"; }
                }
            }
            else
            {
                for (int i = 0; i < volList.Count(); i++)
                {
                    outputTransferVolumeString += Convert.ToString(volList[i]) + ",";
                }
            }
            // Remove trailing comma
            outputTransferVolumeString = outputTransferVolumeString.Substring(0, outputTransferVolumeString.Length - 1);
            return outputTransferVolumeString;
        }

    }
}
