
namespace PuppetMaster
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonBrowseConfigScript = new System.Windows.Forms.Button();
            this.textFileScript = new System.Windows.Forms.TextBox();
            this.textAppDataFile = new System.Windows.Forms.TextBox();
            this.buttonBrowseAppData = new System.Windows.Forms.Button();
            this.textAppInput = new System.Windows.Forms.TextBox();
            this.buttonSendAppData = new System.Windows.Forms.Button();
            this.buttonCreateConnectionWithScheduler = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonBrowseConfigScript
            // 
            this.buttonBrowseConfigScript.Location = new System.Drawing.Point(30, 49);
            this.buttonBrowseConfigScript.Name = "buttonBrowseConfigScript";
            this.buttonBrowseConfigScript.Size = new System.Drawing.Size(111, 25);
            this.buttonBrowseConfigScript.TabIndex = 0;
            this.buttonBrowseConfigScript.Text = "Browse Script";
            this.buttonBrowseConfigScript.UseVisualStyleBackColor = true;
            this.buttonBrowseConfigScript.Click += new System.EventHandler(this.ButtonBrowseConfigScript_Click);
            // 
            // textFileScript
            // 
            this.textFileScript.Location = new System.Drawing.Point(31, 20);
            this.textFileScript.Name = "textFileScript";
            this.textFileScript.Size = new System.Drawing.Size(110, 23);
            this.textFileScript.TabIndex = 1;
            // 
            // textAppDataFile
            // 
            this.textAppDataFile.Location = new System.Drawing.Point(204, 20);
            this.textAppDataFile.Name = "textAppDataFile";
            this.textAppDataFile.Size = new System.Drawing.Size(110, 23);
            this.textAppDataFile.TabIndex = 2;
            // 
            // buttonBrowseAppData
            // 
            this.buttonBrowseAppData.Location = new System.Drawing.Point(203, 49);
            this.buttonBrowseAppData.Name = "buttonBrowseAppData";
            this.buttonBrowseAppData.Size = new System.Drawing.Size(111, 25);
            this.buttonBrowseAppData.TabIndex = 3;
            this.buttonBrowseAppData.Text = "Browse App Data";
            this.buttonBrowseAppData.UseVisualStyleBackColor = true;
            this.buttonBrowseAppData.Click += new System.EventHandler(this.ButtonBrowseAppData_Click);
            // 
            // textAppInput
            // 
            this.textAppInput.Location = new System.Drawing.Point(338, 20);
            this.textAppInput.Name = "textAppInput";
            this.textAppInput.Size = new System.Drawing.Size(110, 23);
            this.textAppInput.TabIndex = 4;
            this.textAppInput.Text = "Please Insert Input";
            this.textAppInput.Click += new System.EventHandler(this.TextAppInput_Click);
            // 
            // buttonSendAppData
            // 
            this.buttonSendAppData.Location = new System.Drawing.Point(338, 49);
            this.buttonSendAppData.Name = "buttonSendAppData";
            this.buttonSendAppData.Size = new System.Drawing.Size(111, 25);
            this.buttonSendAppData.TabIndex = 5;
            this.buttonSendAppData.Text = "Send App Data";
            this.buttonSendAppData.UseVisualStyleBackColor = true;
            this.buttonSendAppData.Click += new System.EventHandler(this.ButtonSendAppData_Click);
            // 
            // buttonCreateConnectionWithScheduler
            // 
            this.buttonCreateConnectionWithScheduler.Location = new System.Drawing.Point(66, 194);
            this.buttonCreateConnectionWithScheduler.Name = "buttonCreateConnectionWithScheduler";
            this.buttonCreateConnectionWithScheduler.Size = new System.Drawing.Size(130, 56);
            this.buttonCreateConnectionWithScheduler.TabIndex = 6;
            this.buttonCreateConnectionWithScheduler.Text = "Create Connection With Scheduler ";
            this.buttonCreateConnectionWithScheduler.UseVisualStyleBackColor = true;
            this.buttonCreateConnectionWithScheduler.Click += new System.EventHandler(this.ButtonCreateConnectionWithScheduler_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonCreateConnectionWithScheduler);
            this.Controls.Add(this.buttonSendAppData);
            this.Controls.Add(this.textAppInput);
            this.Controls.Add(this.buttonBrowseAppData);
            this.Controls.Add(this.textAppDataFile);
            this.Controls.Add(this.textFileScript);
            this.Controls.Add(this.buttonBrowseConfigScript);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonBrowseConfigScript;
        private System.Windows.Forms.TextBox textFileScript;
        private System.Windows.Forms.TextBox textAppDataFile;
        private System.Windows.Forms.Button buttonBrowseAppData;
        private System.Windows.Forms.TextBox textAppInput;
        private System.Windows.Forms.Button buttonSendAppData;
        private System.Windows.Forms.Button buttonCreateConnectionWithScheduler;
    }
}

