using System;
using System.IO;
using System.Net;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Yandex
{
    /// <summary>Получение адреса по координатам (https://tech.yandex.ru/maps/doc/geocoder/)</summary>
    public class GeoDecoder
    {
        /// <summary>Yandex Maps API-key</summary>
        private readonly string Key;
        private const string Url = "https://geocode-maps.yandex.ru/1.x/";
        public Lang lang = Lang.ru_RU;

        public GeoDecoder(string key = null) { Key = key; }

        /// <summary>Получение адреса по координатам</summary>
        public Address GetAddressByPoint(double latitude, double longitude, Lang lang = Lang.ru_RU)
        {
            NameValueCollection query = new NameValueCollection
            {
                { "geocode", $"{latitude.ToString(CultureInfo.InvariantCulture)},{longitude.ToString(CultureInfo.InvariantCulture)}" },
                { "results", "1" },
                { "sco", CoordFormat.latlong.ToString() },
            };
            ApiResponse.ResponseData.YGeoObjectCollection.FeatureMember[] featureMembers = DoRequest(query);
            if (featureMembers == null || featureMembers.Length == 0 || featureMembers[0]?.GeoObject == null)
                return null;

            return ParseGeoObject(featureMembers[0].GeoObject);
        }

        /// <summary>Получение координат и дополнительной информации по адресу</summary>
        /// <param name="address">Строка адреса</param>
        /// <param name="qty">Ограничение количества найденных позиций</param>
        public Address[] GetPointsByAddress(string address, byte qty = 5, Lang lang = Lang.ru_RU)
        {
            if (string.IsNullOrEmpty(address))
                return null;
            NameValueCollection query = new NameValueCollection
            {
                { "geocode", address },
                { "results", (qty == 0 ? 1 : (qty > 100 ? 100 : qty)).ToString() },
            };
            ApiResponse.ResponseData.YGeoObjectCollection.FeatureMember[] featureMembers = DoRequest(query);
            if (featureMembers == null || featureMembers.Length == 0)
                return null;

            List<Address> positions = new List<Address>();
            foreach (var featureMember in featureMembers)
                positions.Add(ParseGeoObject(featureMember.GeoObject));

            return positions.ToArray();
        }

        private Address ParseGeoObject(ApiResponse.ResponseData.YGeoObjectCollection.FeatureMember.YGeoObject geoObject)
        {
            if (geoObject == null)
                return null;
            var geoAddress = geoObject.metaDataProperty?.GeocoderMetaData?.Address;
            var Envelope = geoObject.boundedBy?.Envelope;
            var Point = geoObject.Point;
            Address address = new Address();
            if (geoAddress != null)
            {
                address.CountryCode = geoAddress.country_code;
                address.PostalCode = geoAddress.postal_code;
                address.Precision = geoObject.metaDataProperty?.GeocoderMetaData?.precision;
                address.Formatted = geoAddress.formatted;
                if (geoAddress.Components != null)
                {
                    foreach (var component in geoAddress.Components)
                    {
                        Kind kind;
                        if (!Enum.TryParse(component.kind, out kind))
                            continue;
                        switch (kind)
                        {
                            case Kind.country:
                                address.Country = component.name;
                                break;
                            case Kind.province:
                                if (string.IsNullOrEmpty(address.Province))
                                    address.Province = component.name;
                                else
                                    address.Region = component.name;
                                break;
                            case Kind.area:
                                address.Area = component.name;
                                break;
                            case Kind.locality:
                                address.City = component.name;
                                break;
                            case Kind.street:
                                address.Street = component.name;
                                break;
                            case Kind.house:
                                address.House = component.name;
                                break;
                        }
                    }
                }
            }
            if (Envelope != null)
            {
                address.Envelope = new Envelope
                {
                    LowerCorner = new Point(CoordFormat.longlat, Envelope.lowerCorner),
                    UpperCorner = new Point(CoordFormat.longlat, Envelope.upperCorner),
                };
            }
            if (Point != null && !string.IsNullOrEmpty(Point.pos))
            {
                address.Point = new Point(CoordFormat.longlat, Point.pos);
            }

            return address;
        }

        private ApiResponse.ResponseData.YGeoObjectCollection.FeatureMember[] DoRequest(NameValueCollection query)
        {
            Uri cUri = new Uri(Url);
            query.Add("format", "json");
            query.Add("lang", lang.ToString());
            if (!string.IsNullOrEmpty(Key))
                query.Add("key", Key);
            string strQuery = string.Join("&", query.AllKeys.Select(key => key + "=" + HttpUtility.UrlEncode(query[key])));
            int timeout_in_sec = 60;
            ApiResponse.ResponseData.YGeoObjectCollection.FeatureMember[] featureMembers = null;
            try
            {
                HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(Url + "?" + strQuery);
                webRequest.Host = cUri.Host;
                webRequest.Accept = "*/*";
                webRequest.UserAgent = "YandexGeoLocation client";
                webRequest.Method = "GET";
                webRequest.Timeout = timeout_in_sec * 1000;

                using (WebResponse webResponse = webRequest.GetResponse())
                {
                    if (webResponse == null)
                        throw new ApplicationException("Response null");

                    using (Stream responseStream = webResponse.GetResponseStream())
                    using (StreamReader sr = new StreamReader(responseStream))
                    {
                        string strContent = sr.ReadToEnd().Trim();
                        ApiResponse res = new JavaScriptSerializer().Deserialize<ApiResponse>(strContent);
                        if (res == null || res.response == null)
                            throw new ApplicationException(strContent);
                        if (res.response.GeoObjectCollection != null && res.response.GeoObjectCollection.featureMember != null)
                            featureMembers = res.response.GeoObjectCollection?.featureMember;
                    }
                }
            }
            catch (Exception ex) { throw new ApplicationException($"Error: {ex.Message}.\nRequest: {strQuery}.", ex); }
            return featureMembers;
        }

        public class ApiResponse
        {
            public ResponseData response;
            public class ResponseData
            {
                public YGeoObjectCollection GeoObjectCollection;
                public class YGeoObjectCollection
                {
                    public MetaDataProperty metaDataProperty;
                    public FeatureMember[] featureMember;
                    public class MetaDataProperty
                    {
                        public YGeocoderResponseMetaData GeocoderResponseMetaData;
                        public class YGeocoderResponseMetaData
                        {
                            public string request;// "Москва, улица Новый Арбат, дом 24",
                            public string found;// "1",
                            public string results;// "10"
                        }
                    }
                    public class FeatureMember
                    {
                        public YGeoObject GeoObject;
                        public class YGeoObject
                        {
                            public YMetaDataProperty metaDataProperty;
                            public string description;
                            public string name;
                            public Bound boundedBy;
                            public YPoint Point;
                            public class YMetaDataProperty
                            {
                                public YGeocoderMetaData GeocoderMetaData;
                                public class YGeocoderMetaData
                                {
                                    public string kind;// "house",
                                    public string text;// "Россия, Москва, улица Новый Арбат, 24",
                                    public string precision;// "exact",
                                    public GeoAddress Address;
                                    public class GeoAddress
                                    {
                                        public string country_code;// "RU",
                                        public string postal_code;// "119019",
                                        public string formatted;// "Москва, улица Новый Арбат, 24",
                                        public Component[] Components;
                                        public class Component
                                        {
                                            public string kind;
                                            public string name;
                                        }
                                    }
                                }
                            }
                            public class Bound
                            {
                                public YEnvelope Envelope;
                                public class YEnvelope
                                {
                                    public string lowerCorner;// "37.583508 55.750768"
                                    public string upperCorner;// "37.591719 55.755398"
                                }
                            }
                            public class YPoint
                            {
                                /// <summary>longitude,latitude</summary>
                                public string pos;// "37.587614 55.753083"
                            }
                        }
                    }
                }
            }
        }

        private enum Kind { other, house, street, metro, district, locality, area, province, country, hydro, railway, route, vegetation, airport }
        public enum Lang { ru_RU, uk_UA, be_BY, en_US, en_BR, tr_TR }
        public enum Precision
        {
            other,
            /// <summary>Точное соответствие</summary>
            exact,
            /// <summary>Совпал номер дома, но не совпало строение или корпус.</summary>
            number,
            /// <summary>Найден дом с номером, близким к запрошенному.</summary>
            near,
            /// <summary>Ответ содержит приблизительные координаты запрашиваемого дома.</summary>
            range,
            /// <summary>Найдена только улица.</summary>
            street,
        }
        public enum CoordFormat { longlat, latlong }
        public class Address
        {
            public string CountryCode = string.Empty;
            public string PostalCode = string.Empty;
            public string Country = string.Empty;
            public string Province = string.Empty;
            public string Region = string.Empty;
            public string Area = string.Empty;
            public string City = string.Empty;
            public string Street = string.Empty;
            public string House = string.Empty;
            public string Precision = string.Empty;
            public string Formatted = string.Empty;

            public Envelope Envelope;
            public Point Point;
        }
        public class Envelope
        {
            public Point LowerCorner;
            public Point UpperCorner;
        }
        public class Point
        {
            /// <summary>Широта в градусах. Имеет десятичное представление с точностью до семи знаков после запятой.</summary>
            public double latitude;//55.743675,
            /// <summary>Долгота в градусах. Имеет десятичное представление с точностью до семи знаков после запятой.</summary>
            public double longitude;//37.5646301,

            /// <summary>latitude,longitude</summary>
            /// <returns></returns>
            public override string ToString()
            {
                return $"{latitude},{longitude}";
            }
            /// <summary>Parse coordinates</summary>
            public Point(CoordFormat format, string coords)
            {
                string[] parts = (coords ?? string.Empty).Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                    throw new ArgumentException("Coordinates are wrong");
                string strLatitude = string.Empty;
                string strLongitude = string.Empty;
                switch (format)
                {
                    case CoordFormat.latlong:
                        strLatitude = parts[0];
                        strLongitude = parts[1];
                        break;
                    case CoordFormat.longlat:
                        strLongitude = parts[0];
                        strLatitude = parts[1];
                        break;
                    default:
                        throw new ArgumentException("format is wrong");
                }
                if (!double.TryParse(strLatitude, NumberStyles.Float, CultureInfo.InvariantCulture, out latitude))
                    throw new ArgumentException("Latitude is wrong");
                latitude = Math.Round(latitude, 7);
                if (!double.TryParse(strLongitude, NumberStyles.Float, CultureInfo.InvariantCulture, out longitude))
                    throw new ArgumentException("Longitude is wrong");
                longitude = Math.Round(longitude, 7);
            }
        }
    }
}
