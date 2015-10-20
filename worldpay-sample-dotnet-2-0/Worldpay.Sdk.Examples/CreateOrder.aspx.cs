﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Worldpay.Sdk.Examples;
using Worldpay.Sdk.Enums;
using Worldpay.Sdk.Models;

namespace Worldpay.Sdk.Examples
{
    public partial class CreateOrder : System.Web.UI.Page
    {
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (IsPostBack)
            {
                RequestPanel.Visible = false;
                OnCreateOrder(this, null);
            }
        }

        protected void OnCreateOrder(object sender, CommandEventArgs e)
        {
            var form = HttpContext.Current.Request.Form;
            var orderType = (OrderType)Enum.Parse(typeof(OrderType), form["orderType"]);

            if (orderType == OrderType.APM)
            {
                createAPMOrder();
                return;
            }
            else
            {
                createOrder();
                return;
            }
        }

        private void createOrder()
        {
            var form = HttpContext.Current.Request.Form;
            var client = new WorldpayRestClient((string)Session["service_key"]);
            var orderType = (OrderType)Enum.Parse(typeof(OrderType), form["orderType"]);

            var cardRequest = new CardRequest();
            cardRequest.cardNumber = form["number"];
            cardRequest.cvc = form["cvv"];
            cardRequest.name = form["name"];
            cardRequest.expiryMonth = Convert.ToInt32(form["exp-month"]);
            cardRequest.expiryYear = Convert.ToInt32(form["exp-year"]);
            cardRequest.type = form["cardType"];

            var billingAddress = new Address()
            {
                address1 = form["address1"],
                address2 = form["address2"],
                address3 = form["address3"],
                postalCode = form["postcode"],
                city = form["city"],
                state = "",
                countryCode = Enum.Parse(typeof(CountryCode), form["countryCode"]).ToString()
            };

            var is3DS = form["3ds"] == "on" ? true : false;
            ThreeDSecureInfo threeDSInfo = null;
            if (is3DS)
            {
                var httpRequest = HttpContext.Current.Request;
                threeDSInfo = new ThreeDSecureInfo()
                {
                    shopperIpAddress = httpRequest.UserHostAddress,
                    shopperSessionId = HttpContext.Current.Session.SessionID,
                    shopperUserAgent = httpRequest.UserAgent,
                    shopperAcceptHeader = String.Join(";", httpRequest.AcceptTypes)
                };
            }

            var request = new OrderRequest()
            {
                token = form["token"],
                orderDescription = form["description"],
                amount = (int)(Convert.ToDecimal(form["amount"]) * 100),
                currencyCode = Enum.Parse(typeof(CurrencyCode), form["currency"]).ToString(),
                name =  is3DS ? "3D" : form["name"],
                billingAddress = billingAddress,
                threeDSecureInfo = is3DS ? threeDSInfo : new ThreeDSecureInfo(),
                is3DSOrder = is3DS,
                authorizeOnly = form["authoriseOnly"] == "on",
                orderType = orderType.ToString(),
                customerIdentifiers = new Dictionary<string, string> { { "my-customer-ref", "customer-ref" } },
                customerOrderCode = "A123"
            };

            try
            {
                var response = client.GetOrderService().Create(request);

                HandleSuccessResponse(response);

                SuccessPanel.Visible = true;
            }
            catch (WorldpayException exc)
            {
                ErrorControl.DisplayError(exc.apiError);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Error sending request with token " + request.token, exc);
            }
        }

        private void createAPMOrder()
        {
            var form = HttpContext.Current.Request.Form;
            var client = new WorldpayRestClient((string)Session["service_key"]);

            var billingAddress = new Address()
            {
                address1 = form["address1"],
                address2 = form["address2"],
                address3 = form["address3"],
                postalCode = form["postcode"],
                city = form["city"],
                state = "",
                countryCode = Enum.Parse(typeof(CountryCode), form["countryCode"]).ToString()
            };


            var request = new OrderRequest()
            {
                token = form["token"],
                name = form["name"],
                orderDescription = form["description"],
                amount = (int)(Convert.ToDecimal(form["amount"]) * 100),
                currencyCode = Enum.Parse(typeof(CurrencyCode), form["currency"]).ToString(),
                billingAddress = billingAddress,
                customerIdentifiers = new Dictionary<string, string> { { "my-customer-ref", "customer-ref" } },
                customerOrderCode = "A123",
                successUrl = form["successUrl"],
                failureUrl = form["failureUrl"],
                pendingUrl = form["pendingUrl"],
                cancelUrl = form["cancelUrl"]
            };

            try
            {
                var response = client.GetOrderService().Create(request);

                HandleSuccessResponse(response);

                SuccessPanel.Visible = true;
            }
            catch (WorldpayException exc)
            {
                ErrorControl.DisplayError(exc.apiError);
            }
            catch (Exception exc)
            {
                throw new InvalidOperationException("Error sending request with token " + request.token, exc);
            }
        }

        private void HandleSuccessResponse(OrderResponse response)
        {
            if (response.paymentStatus == OrderStatus.PRE_AUTHORIZED && response.paymentResponse.type == OrderType.APM.ToString())
            {
                HandleAPMResponse(response);
                return;
            }
            if (response.paymentStatus == OrderStatus.PRE_AUTHORIZED && response.is3DSOrder)
            {
                Handle3DSResponse(response);
                return;
            }

            ResponseOrderCode.Text = response.orderCode;
            ResponseToken.Text = response.token;
            ResponsePaymentStatus.Text = response.paymentStatus.ToString();
            ResponseJson.Text = JsonUtils.SerializeObject(response);
        }

        private void HandleAPMResponse(OrderResponse response)
        {
            Session["orderCode"] = response.orderCode;
            Response.Clear();

            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.Append("<body>");
            sb.AppendFormat("<script>\n" + "window.location.replace(\" {0} \");" + "</script>", response.redirectURL);
            sb.Append("</body>");
            sb.Append("</html>");

            Response.Write(sb.ToString());
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }

        private void Handle3DSResponse(OrderResponse response)
        {
            Session["orderCode"] = response.orderCode;
            Response.Clear();

            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.AppendFormat(@"<body onload='document.forms[""submitForm""].submit()'>");
            sb.AppendFormat("<form name='submitForm' action='{0}' method='post'>", response.redirectURL);
            sb.AppendFormat("<input type='hidden' name='PaReq' value='{0}'>", response.oneTime3DsToken);
            sb.AppendFormat("<input type='hidden' name='TermUrl' id='termUrl' value='{0}'>", response.redirectURL);
            sb.Append("<script>\n" +
                      "  document.getElementById('termUrl').value =\n" +
                      "  window.location.href.replace('CreateOrder', 'AuthorizeOrder');" +
                      "</script>");
            sb.Append("</form>");
            sb.Append("</body>");
            sb.Append("</html>");

            Response.Write(sb.ToString());
            HttpContext.Current.ApplicationInstance.CompleteRequest();
        }
    }
}