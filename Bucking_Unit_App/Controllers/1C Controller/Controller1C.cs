using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Xml;
using Bucking_Unit_App.Models;

namespace Bucking_Unit_App._1C_Controller
{
    public class Controller1C
    {
        public async Task<Employee1CModel> GetResp1CSKUD(string cardNumber)
        {
            string xmlPattern = await File.ReadAllTextAsync("employee_data.xml");
            string soapEnvelope = xmlPattern.Replace("CardNumber", cardNumber);

            string url = "http://192.168.12.25/ITPZ_ST/ru_RU/ws/emp_data";

            var handler = new HttpClientHandler
            {
                Credentials = new CredentialCache
                {
                    {
                        new Uri(url), "Basic", new NetworkCredential("obmen", "ghbrjk")
                    }
                    //{
                    //    new Uri(url), "NTLM", new NetworkCredential("", "") // Не знаю нужно или нет, но оставил на основе старой функции.
                    //}
                }
            };

            using HttpClient client = new HttpClient(handler);

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("SOAPAction", "\"emp_data#emp_data:export_data\"");
            request.Content = new StringContent(soapEnvelope, Encoding.UTF8, "text/xml");

            HttpResponseMessage response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return new Employee1CModel()
                {
                    ErrorCode = (int)Employee1CModel.ErrorCodes.SpecificError,
                    ErrorText = $"Ошибка: {response.StatusCode}"
                };
            }

            string soapXml = await response.Content.ReadAsStringAsync();

            Employee1CModel? employee = ParseSoapResponse(soapXml, cardNumber);

            return employee != null ? employee : new Employee1CModel() { ErrorCode = (int)Employee1CModel.ErrorCodes.UnknownError, ErrorText = "Неизвестная ошибка." };
        }

        private Employee1CModel? ParseSoapResponse(string soapXml, string cardNumber)
        {
            // Парсим XML
            XmlDocument xmlDocument = new XmlDocument();
            string? jsonString;

            try
            {
                xmlDocument.LoadXml(soapXml);

                XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(xmlDocument.NameTable);
                xmlNamespaceManager.AddNamespace("soap", "http://schemas.xmlsoap.org/soap/envelope/");
                xmlNamespaceManager.AddNamespace("m", "emp_data");

                // Достаём JSON-строку из тега <m:return>
                jsonString = xmlDocument.SelectSingleNode("//m:return", xmlNamespaceManager)?.InnerText;
            }
            catch (Exception ex)
            {
                return new Employee1CModel() { ErrorCode = (int)Employee1CModel.ErrorCodes.SpecificError, ErrorText = ex.Message };
            }

            if (string.IsNullOrWhiteSpace(jsonString))
                return new Employee1CModel() { ErrorCode = (int)Employee1CModel.ErrorCodes.UnknownError, ErrorText = "Неизвестная ошибка." };

            Employee1CModel? employee = new Employee1CModel();

            try
            {
                // Десериализуем JSON в объект
                employee = JsonSerializer.Deserialize<Employee1CModel>(jsonString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return new Employee1CModel() { ErrorCode = (int)Employee1CModel.ErrorCodes.EmployeeNotFound, ErrorText = "Работник не найден." };
            }

            employee.CardNumber = cardNumber;

            return employee;
        }
    }
}
