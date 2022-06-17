using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.Logging;
using System;
using Venyou;

namespace SamplePlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Sample Plugin";

        private const string showMenu = "/venues";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        private static HttpClient client = new HttpClient();

        private System.Timers.Timer refreshVenuesTimer = new System.Timers.Timer();

        public static string VenueContent { get; set; }
        public static List<VenueModelPost> VenueModels = new List<VenueModelPost>();

        public VenueModelPost ConvertResponseToPost(VenueModelResponse response)
        {
            return new VenueModelPost
            {
                description = response.description ?? "",
                location = response.location ?? "",
                name = response.name ?? "",
                opening_times = response.opening_times ?? "",
                status = response.status ?? false
            };
        }

        private static string requestUrl = "https://venyou.gigalixirapp.com/api/venue/";

        private void UpdateListings(Object source, System.Timers.ElapsedEventArgs e)
        {
            Thread listingThread = new Thread(new ThreadStart(VenueUpdateCoroutine));
            listingThread.Start();
        }

        public static void PostVenueData(VenueModelPost model, Configuration configuration, int? id)
        {
            Thread listingThread = new Thread(delegate () { 
                PostVenueDataCoroutine(model, configuration, id);
            });
            listingThread.Start();
        }

        private static async void PostVenueDataCoroutine(VenueModelPost model, Configuration configutation, int? id)
        {
            try
            {
                PluginLog.Log("Trying to post data.");
                if (id != null)
                {
                    PluginLog.Log("Have ID");
                    var content = new StringContent(JsonConvert.SerializeObject(model), System.Text.Encoding.UTF8, "application/json");
                    var result = await client.PutAsync(requestUrl + id.ToString(), content);
                    string resultContent = await result.Content.ReadAsStringAsync();
                    PluginLog.Log(resultContent);
                }
                else
                {
                    PluginLog.Log("Don't have ID");
                    var content = new StringContent(JsonConvert.SerializeObject(model), System.Text.Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(requestUrl, content);
                    string resultContent = await result.Content.ReadAsStringAsync();
                    
                    if (!string.IsNullOrEmpty(resultContent))
                    {
                        var venueResponse = JsonConvert.DeserializeObject<VenueModelResponse>(resultContent);

                        configutation.VenueId = venueResponse.id;

                        configutation.Save();

                        PluginLog.Log(resultContent);

                    }
                }
            }
            catch (NullReferenceException ex)
            {
                Dalamud.Logging.PluginLog.Log(ex.Message);
            }
        }

        private async void VenueUpdateCoroutine()
        {
            Dalamud.Logging.PluginLog.Log("Attempting to get the venues...");
            try
            {
                var result = await client.GetStringAsync(requestUrl);
                if (result != null)
                {
                    var venueObjects = JsonConvert.DeserializeObject<List<Venyou.VenueModelResponse>>(result.ToString());
                    List<VenueModelPost> convertedVenues = new List<VenueModelPost>();

                    foreach (var venue in venueObjects)
                    {
                        //Dalamud.Logging.PluginLog.Log("Name : " + (venue.description ?? "null"));
                        convertedVenues.Add(ConvertResponseToPost(venue));
                    }

                    VenueModels = convertedVenues;
                }
            }
            catch(NullReferenceException ex)
            {
                Dalamud.Logging.PluginLog.Log(ex.Message);
            }
        }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            if(this.Configuration.UserId == null)
            {
                this.Configuration.UserId = Guid.NewGuid();
                this.Configuration.Save();
            }

            // you might normally want to embed resources and load them from the manifest stream
            var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);
            this.PluginUi = new PluginUI(this.Configuration, goatImage);

            this.CommandManager.AddHandler(showMenu, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            //TODO : Make member variable to dispose
            
            refreshVenuesTimer.Interval = 30000;
            refreshVenuesTimer.AutoReset = true;
            refreshVenuesTimer.Elapsed += UpdateListings;
            refreshVenuesTimer.Enabled = true;
        }

        public void Dispose()
        {
            this.PluginUi.Dispose();
            this.CommandManager.RemoveHandler(showMenu);
            refreshVenuesTimer.Dispose();
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            this.PluginUi.Visible = true;
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
