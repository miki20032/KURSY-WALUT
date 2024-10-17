using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace KURSY_WALUT
{
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();
        private const string BaseUrl = "http://api.nbp.pl/api/exchangerates/tables/A?format=json";
        private List<Rate> currentRates;

        public MainWindow()
        {
            InitializeComponent();
            LoadCurrencyRates();
        }

        private async void LoadCurrencyRates()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(BaseUrl);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                var rates = JsonConvert.DeserializeObject<List<ExchangeRateTable>>(responseBody);
                currentRates = rates[0].Rates;
                CurrencyComboBox.ItemsSource = currentRates.Select(rate => rate.Currency);
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show("Wystąpił błąd HTTP: " + httpEx.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił nieznany błąd: " + ex.Message);
            }
        }

        private void CurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedCurrency = CurrencyComboBox.SelectedItem as string;
            if (selectedCurrency != null)
            {
                var selectedRate = currentRates.FirstOrDefault(rate => rate.Currency == selectedCurrency);
                if (selectedRate != null)
                {
                    CurrencyRateText.Text = $"{selectedRate.Currency} ({selectedRate.Code}): {selectedRate.Mid}";
                }
            }
        }

        private void ConvertButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCurrency = CurrencyComboBox.SelectedItem as string;
            if (selectedCurrency != null && decimal.TryParse(AmountTextBox.Text, out decimal amount))
            {
                var selectedRate = currentRates.FirstOrDefault(rate => rate.Currency == selectedCurrency);
                if (selectedRate != null)
                {
                    var convertedAmount = amount * selectedRate.Mid;
                    ConversionResultText.Text = $"{amount} {selectedRate.Currency} = {convertedAmount} PLN";
                }
                else
                {
                    ConversionResultText.Text = "Nie można znaleźć kursu waluty.";
                }
            }
            else
            {
                ConversionResultText.Text = "Proszę wprowadzić prawidłową kwotę.";
            }
        }

        private void ConvertToPLNButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedCurrency = CurrencyComboBox.SelectedItem as string;
            if (selectedCurrency != null && decimal.TryParse(AmountToPLNTextBox.Text, out decimal amount))
            {
                var selectedRate = currentRates.FirstOrDefault(rate => rate.Currency == selectedCurrency);
                if (selectedRate != null)
                {
                    var convertedAmount = amount / selectedRate.Mid;
                    ConversionToPLNResultText.Text = $"{amount} PLN = {convertedAmount} {selectedRate.Currency}";
                }
                else
                {
                    ConversionToPLNResultText.Text = "Nie można znaleźć kursu waluty.";
                }
            }
            else
            {
                ConversionToPLNResultText.Text = "Proszę wprowadzić prawidłową kwotę.";
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (FileTypeComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string selectedFileType = selectedItem.Content.ToString().ToLower();
                    string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                    string todayDate = DateTime.Now.ToString("dd-MM-yyyy");
                    string filePath = Path.Combine(downloadsPath, $"kursy walut z {todayDate}.{selectedFileType}");

                    using (StreamWriter sw = new StreamWriter(filePath))
                    {
                        if (selectedFileType == "csv")
                        {
                            sw.WriteLine("Currency,Code,Rate");
                            foreach (var rate in currentRates)
                            {
                                sw.WriteLine($"{rate.Currency},{rate.Code},{rate.Mid}");
                            }
                        }
                        else if (selectedFileType == "txt")
                        {
                            sw.WriteLine("Currency\tCode\tRate");
                            foreach (var rate in currentRates)
                            {
                                sw.WriteLine($"{rate.Currency}\t{rate.Code}\t{rate.Mid}");
                            }
                        }
                    }

                    MessageBox.Show($"Kursy walut zapisane do pliku {filePath}");
                }
                else
                {
                    MessageBox.Show("Proszę wybrać format pliku.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Wystąpił błąd podczas zapisu do pliku: " + ex.Message);
            }
        }
    }

    public class ExchangeRateTable
    {
        public string Table { get; set; }
        public string No { get; set; }
        public string EffectiveDate { get; set; }
        public List<Rate> Rates { get; set; }
    }

    public class Rate
    {
        public string Currency { get; set; }
        public string Code { get; set; }
        public decimal Mid { get; set; }
    }
}
