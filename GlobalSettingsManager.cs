﻿using System;
using System.IO;
using System.Windows.Forms;
using Desktoptale.Messages;
using Desktoptale.Messaging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Desktoptale
{
    public class GlobalSettingsManager
    {
        public GlobalSettings GlobalSettings;
        
        private string path;
        private ISerializer serializer;
        private IDeserializer deserializer;

        private bool disableSaving;

        public GlobalSettingsManager(string path)
        {
            this.path = path;
            
            serializer = new SerializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .Build();
            
            deserializer = new DeserializerBuilder()
                .WithNamingConvention(PascalCaseNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            MessageBus.Subscribe<ClickThroughChangedMessage>(OnClickThroughChangedMessage);
            MessageBus.Subscribe<InteractionButtonChangedMessage>(OnInteractionButtonChangedMessage);
        }

        public bool DoesGlobalSettingsFileExist()
        {
            return File.Exists(path);
        }

        public void SendMessages()
        {
            disableSaving = true;
            
            MessageBus.Send(new ClickThroughChangedMessage() { Enabled = GlobalSettings.ClickThroughMode });
            MessageBus.Send(new InteractionButtonChangedMessage() { Enabled = GlobalSettings.EnableInteractionButton });

            disableSaving = false;
        }
        
        public void LoadGlobalSettings()
        {
            try
            {
                string serialized = File.ReadAllText(path);
                GlobalSettings = deserializer.Deserialize<GlobalSettings>(serialized);
            }
            catch (Exception e)
            {
                WindowsUtils.ShowMessageBox($"Failed to load global settings: {e.Message}", ProgramInfo.NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SaveGlobalSettings()
        {
            if (disableSaving)
            {
                return;
            }

            FileStream fileStream = null;
            try
            {
                if (File.Exists(path))
                {
                    fileStream = File.Open(path, FileMode.Truncate);
                }
                else
                {
                    fileStream = File.Create(path);
                    File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.Hidden);
                }

                string serialized = serializer.Serialize(GlobalSettings);
                
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    writer.Write(serialized);
                }
            }
            catch (Exception e)
            {
                disableSaving = true;

                WindowsUtils.ShowMessageBox(
                    $"Failed to save global settings to file {Path.GetFullPath(path)}\n\nYou can still use the program, but your global settings will not be saved.",
                    ProgramInfo.NAME, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                fileStream?.Dispose();
            }
        }

        private void OnClickThroughChangedMessage(ClickThroughChangedMessage message)
        {
            GlobalSettings.ClickThroughMode = message.Enabled;
            SaveGlobalSettings();
        }

        private void OnInteractionButtonChangedMessage(InteractionButtonChangedMessage message)
        {
            GlobalSettings.EnableInteractionButton = message.Enabled;
            SaveGlobalSettings();
        }
    }
}