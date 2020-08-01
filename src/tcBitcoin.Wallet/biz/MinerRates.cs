using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace TradeControl.Bitcoin
{
    [DataContract]
    internal class BitcoinFees
    {
        [DataMember]
        internal int fastestFee = 0;
        [DataMember]
        internal int halfHourFee = 0;
        [DataMember]
        internal int hourFee = 0;
    }

    public sealed class MinerRates
    {
        public enum MiningSpeed { Fastest, HalfHour, Hour };
        MiningSpeed speed = MiningSpeed.Fastest;

        public int FastestFee { get; private set; }
        public int HalfHourFee { get; private set; }
        public int HourFee { get; private set; }

        const string serviceAddress = "https://bitcoinfees.earn.com/api/v1/fees/recommended";

        public MinerRates(MiningSpeed miningSpeed)
        {
            try
            {
                speed = miningSpeed;

                WebClient webClient = new WebClient();
                webClient.Proxy = null;
                string json = webClient.DownloadString(serviceAddress);

                var minerRates = GetMinerRates(json);
                FastestFee = minerRates.fastestFee;
                HalfHourFee = minerRates.halfHourFee;
                HourFee = minerRates.hourFee;
            }
            catch (Exception err)
            {
                Console.WriteLine($"{err.Message}", $"{err.Source}.{err.TargetSite.Name}");
            }
        }

        BitcoinFees GetMinerRates(string json)
        {
            var deserializedRates = new BitcoinFees();
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            var ser = new DataContractJsonSerializer(deserializedRates.GetType());
            deserializedRates = ser.ReadObject(ms) as BitcoinFees;
            ms.Close();
            return deserializedRates;
        }

        public int GetFees(int transactionSize)
        {
            int charge = 0;

            switch (speed)
            {
                case MiningSpeed.Fastest:
                    charge = transactionSize * FastestFee;
                    break;
                case MiningSpeed.HalfHour:
                    charge = transactionSize * HalfHourFee;
                    break;
                case MiningSpeed.Hour:
                    charge = transactionSize * HourFee;
                    break;
            }

            return charge;
        }

        public override string ToString()
        {
            return $"Fastest Fee {FastestFee}; Half hour fee: {HalfHourFee}; Hour fee: {HourFee}; ";
        }
    }
}
