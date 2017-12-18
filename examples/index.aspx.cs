using System;
using System.Web.UI;
using System.Web.Script.Serialization;
using Yandex;

namespace GeoLocation
{
    public partial class Location : Page
    {
        private const string YandexKey = "";

        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected string GetPositionByIp(string IpV4)
        {
            GeoLocator.Ip IpAddress = new GeoLocator.Ip { address_v4 = IpV4 };
            string res = $"ip = { new JavaScriptSerializer().Serialize(IpAddress)}\n";

            GeoLocator.Position position = new GeoLocator(YandexKey).GetByIp(IpAddress);
            res += $"Position = { new JavaScriptSerializer().Serialize(position)}\n";
            return res;
        }

        protected string GetPositionByWifi(string mac)
        {
            GeoLocator.WiFi[] wfs = new GeoLocator.WiFi[] { new GeoLocator.WiFi { mac = "00-1C-F0-E4-BB-F5" } };
            string res = $"wifi_networks = { new JavaScriptSerializer().Serialize(wfs)}\n";

            GeoLocator.Position position = new GeoLocator(YandexKey).GetByWiFi(wfs);
            res += $"Position = { new JavaScriptSerializer().Serialize(position)}\n";
            return res;
        }

        protected string GetPositionByGsm(int countrycode, int operatorid, int cellid, int lac)
        {
            GeoLocator.Gsm[] cells = new GeoLocator.Gsm[] {
                new GeoLocator.Gsm {
                    countrycode = countrycode,
                    operatorid = operatorid,
                    cellid = cellid,
                    lac= lac,
                }
            };
            string res = $"gsm_cells = { new JavaScriptSerializer().Serialize(cells)}\n";

            GeoLocator.Position position = new GeoLocator(YandexKey).GetByGsm(cells);
            res += $"Position = { new JavaScriptSerializer().Serialize(position)}\n";
            return res;
        }

        protected string GetAddressByPosition(decimal latitude, decimal longitude)
        {
            string res = $"Position = { new JavaScriptSerializer().Serialize(new { latitude, longitude })}\n";
            GeoDecoder.Address address = new GeoDecoder(YandexKey).GetAddressByPoint(latitude, longitude);
            res += $"Address = { new JavaScriptSerializer().Serialize(address)}\n";
            return res;
        }

        protected string GetPositionsByAddress(string address, byte qty, GeoDecoder.Lang lang)
        {
            string res = $"Address = { new JavaScriptSerializer().Serialize(new { address })}\n";
            GeoDecoder.Address[] positions = new GeoDecoder(YandexKey).GetPointsByAddress(address, qty, lang);
            res += $"Positions = { new JavaScriptSerializer().Serialize(positions)}\n";
            return res;
        }

        protected string GetAddressByIp(string IpV4)
        {
            GeoLocator.Ip IpAddress = new GeoLocator.Ip { address_v4 = IpV4 };
            string res = $"ip = { new JavaScriptSerializer().Serialize(new { IpV4 })}\n";
            GeoLocator.Position position = new GeoLocator(YandexKey).GetByIp(IpAddress);
            GeoDecoder.Address address = position != null ? new GeoDecoder(YandexKey).GetAddressByPoint(position.latitude, position.longitude) : null;
            res += $"Address = { new JavaScriptSerializer().Serialize(address)}\n";
            return res;
        }

        protected string Nl2br(string str)
        {
            return (str ?? string.Empty).Replace("\n", "<br>\n");
        }
    }
}
