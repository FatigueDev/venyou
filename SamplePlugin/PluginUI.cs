using ImGuiNET;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace SamplePlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration configuration;

        private ImGuiScene.TextureWrap goatImage;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        public Venyou.VenueModelPost CreateVenueModel;

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration, ImGuiScene.TextureWrap goatImage)
        {
            this.configuration = configuration;
            this.goatImage = goatImage;

            CreateVenueModel = this.configuration.UserVenue;
        }

        public void Dispose()
        {
            this.goatImage.Dispose();
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
            DrawSettingsWindow();
        }

        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 330), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 330), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("Venue Listings", ref this.visible))
            {
                // ImGui.BeginTable("Venue Table", 2);
                try
                {
                    if (ImGui.BeginTabBar("VenueTabBar")) {
                        if (ImGui.BeginTabItem("List Venues"))
                        {
                            if (Plugin.VenueModels.Any())
                            {
                                var sortedObjects = Plugin.VenueModels.OrderBy(vm => vm.status == false).ToList();
                                sortedObjects.ForEach(venue =>
                                {
                                    if(venue.status == false)
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.8f, 0.1f, 0.1f, 0.8f));
                                    }
                                    else
                                    {
                                        ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.1f, 0.8f, 0.1f, 0.8f));
                                    }

                                    if (ImGui.CollapsingHeader((venue.status == false ? "[CLOSED] - " : "[OPEN] - ") + venue.name))
                                    {
                                        ImGui.PopStyleColor(1);
                                        ImGui.InputTextMultiline("Description", ref venue.description, 255, new Vector2(380, 100), ImGuiInputTextFlags.ReadOnly);
                                        ImGui.InputText("Opening Times", ref venue.opening_times, 255, ImGuiInputTextFlags.ReadOnly);
                                        ImGui.InputText("Location", ref venue.location, 255, ImGuiInputTextFlags.ReadOnly);
                                    }
                                });
                            }

                            ImGui.EndTabItem();
                        }
                        if(ImGui.BeginTabItem("Create Venue"))
                        {
                            ImGui.InputText("Name", ref CreateVenueModel.name, 255);
                            ImGui.InputTextMultiline("Description", ref CreateVenueModel.description, 255, new Vector2(387, 100), ImGuiInputTextFlags.NoHorizontalScroll | ImGuiInputTextFlags.CtrlEnterForNewLine);
                            ImGui.InputText("Opening Times", ref CreateVenueModel.opening_times, 255);
                            ImGui.InputText("Location", ref CreateVenueModel.location, 255);
                            ImGui.Checkbox("Open", ref CreateVenueModel.status);

                            if (ImGui.Button("Save Changes"))
                            {
                                // HTTP post if not exists, update changeset if does.
                                CreateVenueModel.owner = this.configuration.UserId.ToString();

                                Plugin.PostVenueData(CreateVenueModel, this.configuration, this.configuration.VenueId);

                                this.configuration.UserVenue = CreateVenueModel;
                                this.configuration.Save();
                            }
                            ImGui.EndTabItem();
                        }
                        ImGui.EndTabBar();
                    }

                }
                catch (NullReferenceException ex)
                {
                    Dalamud.Logging.PluginLog.Log(ex.Message);
                }

                
                //ImGui.EndTable();

                //ImGui.Text($"The random config bool is {this.configuration.SomePropertyToBeSavedAndWithADefault}");

                //if (ImGui.Button("Show Settings"))
                //{
                //    SettingsVisible = true;
                //}

                //ImGui.Spacing();

                //ImGui.Text("Have a goat:");
                //ImGui.Indent(55);
                //ImGui.Image(this.goatImage.ImGuiHandle, new Vector2(this.goatImage.Width, this.goatImage.Height));
                //ImGui.Unindent(55);
            }
            ImGui.End();
        }

        public void DrawSettingsWindow()
        {
            if (!SettingsVisible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(232, 75), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                // can't ref a property, so use a local copy
                //var configValue = this.configuration.UserVenue;
                //if (ImGui.Checkbox("Random Config Bool", ref configValue))
                //{
                //    this.configuration.SomePropertyToBeSavedAndWithADefault = configValue;
                //    // can save immediately on change, if you don't want to provide a "Save and Close" button
                //    this.configuration.Save();
                //}
            }
            ImGui.End();
        }
    }
}
