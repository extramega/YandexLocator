<%@ page language="C#" autoeventwireup="true" codebehind="index.aspx.cs" inherits="GeoLocation.Location" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Samples</title>
</head>
<body>
    <h3>GetPositionByIp</h3>
    <p>GetPositionByIp("127.0.0.1")</p>
    <h3>GetPositionByIp</h3>
    <p>GetPositionByIp("77.88.55.55")</p>
    <h3>GetPositionByWifi</h3>
    <p>GetPositionByWifi("00-1C-F0-E4-BB-F5")</p>
    <h3>GetPositionByGsm</h3>
    <p>GetPositionByGsm(250,99,42332,36002)</p>
    <h3>GetAddressByPosition</h3>
    <p><%= Nl2br(GetAddressByPosition(55.767175, 37.571327)) %></p>
    <h3>GetPositionsByAddress</h3>
    <p><%= Nl2br(GetPositionsByAddress("San-Francisco", 10, Yandex.GeoDecoder.Lang.en_US)) %></p>
    <h3>GetAddressByIp</h3>
    <p>GetAddressByIp("178.247.233.32")</p>
</body>
</html>
