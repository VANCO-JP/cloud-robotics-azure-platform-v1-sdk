﻿using Microsoft.Azure.Devices;
using Microsoft.Azure.Devices.Common;
using System;
using System.Windows.Forms;

namespace CloudRoboticsDefTool
{
    public partial class CreateDeviceForm : Form
    {
        private string iotHubConnectionString;
        private string sqlConnectionString;
        private int devicesMaxCount;
        private RegistryManager registryManager;
        private bool generateDeviceID;
        private bool generateDeviceKeys;

        public CreateDeviceForm(string iotHubConnectionString, string sqlConnectionString,int devicesMaxCount)
        {
            InitializeComponent();

            this.iotHubConnectionString = iotHubConnectionString;
            this.sqlConnectionString = sqlConnectionString;
            this.registryManager = RegistryManager.CreateFromConnectionString(iotHubConnectionString);
            this.devicesMaxCount = devicesMaxCount;

            generateIDCheckBox.Checked = false;
            generateDeviceID = false;

            generateKeysCheckBox.Checked = true;
            generateDeviceKeys = true;
            autoGenerateDeviceKeys();
        }

        private void autoGenerateDeviceKeys()
        {
            primaryKeyTextBox.Text = CryptoKeyGenerator.GenerateKey(32);
            secondaryKeyTextBox.Text = CryptoKeyGenerator.GenerateKey(32);
        }

        private void autoGenerateDeviceID()
        {
            deviceIDTextBox.Text = "device" + Guid.NewGuid().ToString("N");
        }

        private async void createButton_Click(object sender, EventArgs e)
        {
            if (comboBoxDevMDeviceType.Text == "" || comboBoxDevMStatus.Text == "")
            {
                MessageBox.Show("Both Device Type and Status are required !","** Input Error **",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }

            try
            {
                var device = new Device(deviceIDTextBox.Text);
                await registryManager.AddDeviceAsync(device);
                device = await registryManager.GetDeviceAsync(device.Id);
                device.Authentication.SymmetricKey.PrimaryKey = primaryKeyTextBox.Text;
                device.Authentication.SymmetricKey.SecondaryKey = secondaryKeyTextBox.Text;
                device = await registryManager.UpdateDeviceAsync(device);
                // IoT Hub Device
                var deviceCreated = new DeviceCreatedForm(device.Id, device.Authentication.SymmetricKey.PrimaryKey, device.Authentication.SymmetricKey.SecondaryKey);
                deviceCreated.ShowDialog();

                // RBFX Device Master
                var deviceEntity = new DeviceEntity();
                deviceEntity.Id = device.Id;
                deviceEntity.DevM_DeviceType = comboBoxDevMDeviceType.Text;
                deviceEntity.DevM_Status = comboBoxDevMStatus.Text;
                deviceEntity.DevM_ResourceGroupId = textBoxDevMRescGrpId.Text;
                deviceEntity.DevM_Description = textBoxDevMDesc.Text;
                deviceEntity.DevM_Registered_DateTime = DateTime.Now;
                var dmi = new DeviceMasterInfo(sqlConnectionString, deviceEntity);
                dmi.CreateDevice();

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void generateIDCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (generateDeviceID == true) // was checked prior to the click
            {
                generateDeviceID = false;
                deviceIDTextBox.ResetText();
            }
            else  // was NOT checked prior to the click
            {
                generateDeviceID = true;
                autoGenerateDeviceID();
            }

        }

        private void generateKeysCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (generateDeviceKeys == true) // was checked prior to the click
            {
                generateDeviceKeys = false;
                primaryKeyTextBox.ResetText();
                secondaryKeyTextBox.ResetText();
            }
            else  // was NOT checked prior to the click
            {
                generateDeviceKeys = true;
                autoGenerateDeviceKeys();
            }
        }

        private void CreateDeviceForm_Load(object sender, EventArgs e)
        {
            comboBoxDevMDeviceType.DataSource = CRoboticsConst.DeviceTypeList;
            comboBoxDevMDeviceType.Text = "";
            comboBoxDevMStatus.DataSource = CRoboticsConst.StatusList;
            comboBoxDevMStatus.Text = "";
        }
    }
}
