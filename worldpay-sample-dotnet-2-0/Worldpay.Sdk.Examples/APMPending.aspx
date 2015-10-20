﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="APMPending.aspx.cs" Inherits="Worldpay.Sdk.Examples.APMPending" MasterPageFile="Master.Master" %>

<%@ Register TagPrefix="uc" TagName="ErrorControl" Src="ErrorControl.ascx" %>

<asp:Content ID="ResponsePanel" ContentPlaceHolderID="ContentPlaceHolder" runat="server">
    <h1>.NET 2.0 Library Create Order Example</h1>

    <h2>Payment Response</h2>
    <p>
        <asp:Literal runat="server" ID="OrderResponse"></asp:Literal>
        <uc:ErrorControl ID="ErrorControl" runat="server"/>
    </p>
</asp:Content>