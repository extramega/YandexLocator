YandexLocator
==============
C# module using Yandex API for geolocation

Service specifications
-----------
https://tech.yandex.ru/locator

https://tech.yandex.ru/maps/doc/geocoder

You can get coordinates by IP address only if you have Yandex Key (http://api.yandex.ru/maps/form.xml)

Example
-----------
```
using Yandex;

GeoLocator.Position position = new GeoLocator(YandexKey).GetByIp(new GeoLocator.Ip { address_v4 = "77.88.55.55" });
GeoDecoder.Address[] points = new GeoDecoder().GetPositionsByAddress("San-Francisco", 10);
```

