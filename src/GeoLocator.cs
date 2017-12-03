using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace Yandex
{
    /// <summary>Определение координат по параметрам сети (https://tech.yandex.ru/locator/). Для использования необходим уникальный ключ доступа к Яндекс.Локатор (http://api.yandex.ru/maps/form.xml)</summary>
    public class GeoLocator
    {
        /// <summary>Yandex Maps API-key</summary>
        private readonly string Key;
        private const string Url = "http://api.lbs.yandex.net/geolocation";
        private const string Version = "1.0";
        public GeoLocator(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentOutOfRangeException("Yandex Key is empty. Get key on http://api.yandex.ru/maps/form.xml");
            Key = key;
        }
        /// <summary>Геолокация по IP адресу. Для внутренних адресов возвращается null.</summary>
        public Position GetByIp(Ip IpAddress)
        {
            if (IpAddress == null || string.IsNullOrEmpty(IpAddress.address_v4))
                throw new ArgumentOutOfRangeException("Ip address (v4) is empty");
            if (IsInternalIpV4(IpAddress.address_v4))
                return null;
            Dictionary<string, object> arg = new Dictionary<string, object> { { "ip", IpAddress } };
            return DoRequest(arg);
        }

        /// <summary>Геолокация по мобильным сотам</summary>
        public Position GetByGsm(Gsm[] cells)
        {
            if (cells == null || cells.Length == 0)
                throw new ArgumentOutOfRangeException("cells is empty");
            Dictionary<string, object> arg = new Dictionary<string, object> { { "gsm_cells", cells } };
            return DoRequest(arg);
        }
        /// <summary>Геолокация по точкам доступа</summary>
        public Position GetByWiFi(WiFi[] wifi)
        {
            if (wifi == null || wifi.Length == 0)
                throw new ArgumentOutOfRangeException("wifi is empty");
            Dictionary<string, object> arg = new Dictionary<string, object> { { "wifi_networks", wifi } };
            return DoRequest(arg);
        }

        private Position DoRequest(Dictionary<string, object> requestParams)
        {
            requestParams.Add("common", new { version = Version, api_key = Key });

            Uri cUri = new Uri(Url);
            string strQuery = "json=" + new JavaScriptSerializer().Serialize(requestParams);
            byte[] bytes = Encoding.ASCII.GetBytes(strQuery);
            int timeout_in_sec = 60;
            Position position = null;

            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url);
                webRequest.ContentType = "application/x-www-form-urlencoded";
                webRequest.Host = cUri.Host;
                webRequest.Accept = "*/*";
                webRequest.UserAgent = "YandexGeoLocation client";
                webRequest.Method = "POST";
                webRequest.Timeout = timeout_in_sec * 1000;
                webRequest.ContentLength = bytes.Length;

                using (Stream os = webRequest.GetRequestStream())
                    os.Write(bytes, 0, bytes.Length);

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    if (webResponse == null)
                        throw new ApplicationException("Response null");
                    using (Stream responseStream = webResponse.GetResponseStream())
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        string strContent = sr.ReadToEnd().Trim();
                        ApiResponse response = new JavaScriptSerializer().Deserialize<ApiResponse>(strContent);
                        if (response == null)
                            throw new ApplicationException($"Deserialize error: {strContent}");
                        if (!string.IsNullOrEmpty(response.error))
                            throw new ApplicationException(response.error);
                        if (response.position == null || string.IsNullOrEmpty(response.position.type))
                            throw new ApplicationException(strContent);
                        position = response.position;
                    }
                }
            }
            catch (Exception ex) { throw new ApplicationException($"Error: {ex.Message}.\nRequest: {strQuery}.", ex); }
            return position;
        }
        public static bool IsInternalIpV4(string IpAddressV4)
        {
            return IsInternalIpV4(IpV4StrToBytes(IpAddressV4));
        }
        public static bool IsInternalIpV4(byte[] octets)
        {
            if (octets.Length != 4)
                throw new ArgumentException("Octets array length must be 4");
            switch (octets[0])
            {
                case 0:
                    if (octets[1] == 0 && octets[2] == 0 && octets[3] == 0)
                        return true;//Ошибка
                    break;
                case 10:
                case 127:
                    return true;//A
                case 172:
                    if (octets[1] >= 16 && octets[0] <= 31)
                        return true;//B
                    break;
                case 192:
                    if (octets[1] == 168)
                        return true;//C
                    break;
                case 255:
                    return true;//Broadcast
            }
            return false;
        }
        public static byte[] IpV4StrToBytes(string IpAddressV4)
        {
            byte[] octets = new byte[4];
            if (string.IsNullOrEmpty(IpAddressV4))
                throw new ArgumentException("Ip address is empty");
            string[] parts = IpAddressV4.Split(new[] { '.' });
            if (parts.Length != 4)
                throw new ArgumentException("Ip address is wrong");
            for (int i = 0; i < 4; i++)
            {
                if (!Byte.TryParse(parts[i], out octets[i]))
                    throw new ArgumentException("Ip address is wrong");
            }
            return octets;
        }
        /// <summary>Мобильная сота</summary>
        public class Gsm
        {
            /// <summary>Код страны(MCC, Mobile Country Code).</summary>
            public int countrycode;// = 250;
            /// <summary>Код сети мобильной связи(MNC, Mobile Network Code).</summary>
            public int operatorid;// = 99;
            /// <summary>Идентификатор соты(CID, Cell Identifier).</summary>
            public int cellid;// = 42332;
            /// <summary>Код местоположения(LAC, Location area code).</summary>
            public int lac;// = 36002;
            /// <summary>Уровень сигнала, измеренный в месте нахождения мобильного устройства.Отрицательное число, выраженное в «децибелах к милливатту» — dBm.Элемент зарезервирован для будущего использования.</summary>
            public int? signal_strength;// = -80;
            /// <summary>Время в миллисекундах, прошедшее с момента получения данных через программный интерфейс мобильного устройства. Элемент зарезервирован для будущего использования.</summary>
            public int? age;// = 5555;
        }
        /// <summary>Точка доступа Wi-Fi</summary>
        public class WiFi
        {
            /// <summary>MAC-адрес в символьном представлении. Байты могут разделяться дефисом, точкой, двоеточием или указываться слитно без разделителя, например: «12-34-56-78-9A-BC», «12:34:56:78:9A:BC», «12.34.56.78.9A.BC», «123456789ABC».</summary>
            public string mac;// "00-1C-F0-E4-BB-F5",
            /// <summary>Уровень сигнала, измеренный в месте нахождения мобильного устройства. Отрицательное число, выраженное в «децибелах к милливатту» — dBm. Элемент зарезервирован для будущего использования.</summary>
            public int? signal_strength;// -88,
            /// <summary>Время в миллисекундах, прошедшее с момента получения данных через программный интерфейс мобильного устройства. Элемент зарезервирован для будущего использования.</summary>
            public int? age;// 0,
        }
        /// <summary>IP-адрес</summary>
        public class Ip
        {
            /// <summary>IP-адрес мобильного устройства, назначенный оператором мобильного интернета.</summary>
            public string address_v4;//"178.247.233.32"
        }
        public class ApiResponse
        {
            public string error;
            public Position position;
        }
        public class Position
        {
            /// <summary>Широта в градусах. Имеет десятичное представление с точностью до семи знаков после запятой.</summary>
            public double latitude;//55.743675,
            /// <summary>Долгота в градусах. Имеет десятичное представление с точностью до семи знаков после запятой.</summary>
            public double longitude;//37.5646301,
            /// <summary>Высота над поверхностью мирового океана.</summary>
            public double altitude;//0.0, 
            /// <summary>Максимальное расстояние от указанной точки, в пределах которого находится мобильное устройство.</summary>
            public double precision;//701.71643,
            /// <summary>Максимальное отклонение от указанной высоты.</summary>
            public double altitude_precision;//30.0, 
            /// <summary>Обозначение способа, которым определено местоположение: «gsm» — по сотам мобильных сетей, «wifi» — по точкам доступа Wi-Fi, «ip» — по IP-адресу.</summary>
            public string type;//"ip"
        }
        public enum PositionType { ip, wifi, gsm }
    }
}
