
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
            this.textBoxScript = new System.Windows.Forms.RichTextBox();
            this.buttonNextStep = new System.Windows.Forms.Button();
            this.textBoxDebug = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
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
            this.textAppDataFile.Location = new System.Drawing.Point(31, 139);
            this.textAppDataFile.Name = "textAppDataFile";
            this.textAppDataFile.Size = new System.Drawing.Size(110, 23);
            this.textAppDataFile.TabIndex = 2;
            // 
            // buttonBrowseAppData
            // 
            this.buttonBrowseAppData.Location = new System.Drawing.Point(31, 168);
            this.buttonBrowseAppData.Name = "buttonBrowseAppData";
            this.buttonBrowseAppData.Size = new System.Drawing.Size(111, 25);
            this.buttonBrowseAppData.TabIndex = 3;
            this.buttonBrowseAppData.Text = "Browse App Data";
            this.buttonBrowseAppData.UseVisualStyleBackColor = true;
            this.buttonBrowseAppData.Click += new System.EventHandler(this.ButtonBrowseAppData_Click);
            // 
            // textAppInput
            // 
            this.textAppInput.Location = new System.Drawing.Point(31, 208);
            this.textAppInput.Name = "textAppInput";
            this.textAppInput.Size = new System.Drawing.Size(110, 23);
            this.textAppInput.TabIndex = 4;
            this.textAppInput.Text = "Please Insert Input";
            this.textAppInput.Click += new System.EventHandler(this.TextAppInput_Click);
            // 
            // buttonSendAppData
            // 
            this.buttonSendAppData.Location = new System.Drawing.Point(31, 237);
            this.buttonSendAppData.Name = "buttonSendAppData";
            this.buttonSendAppData.Size = new System.Drawing.Size(111, 25);
            this.buttonSendAppData.TabIndex = 5;
            this.buttonSendAppData.Text = "Send App Data";
            this.buttonSendAppData.UseVisualStyleBackColor = true;
            this.buttonSendAppData.Click += new System.EventHandler(this.ButtonSendAppData_Click);
            // 
            // textBoxScript
            // 
            this.textBoxScript.Location = new System.Drawing.Point(533, 41);
            this.textBoxScript.Name = "textBoxScript";
            this.textBoxScript.Size = new System.Drawing.Size(242, 350);
            this.textBoxScript.TabIndex = 6;
            this.textBoxScript.Text = "";
            // 
            // buttonNextStep
            // 
            this.buttonNextStep.Location = new System.Drawing.Point(617, 12);
            this.buttonNextStep.Name = "buttonNextStep";
            this.buttonNextStep.Size = new System.Drawing.Size(75, 23);
            this.buttonNextStep.TabIndex = 7;
            this.buttonNextStep.Text = "Next Step";
            this.buttonNextStep.UseVisualStyleBackColor = true;
            this.buttonNextStep.Click += new System.EventHandler(this.ButtonNextStep_Click);
            // 
            // textBoxDebug
            // 
            this.textBoxDebug.Location = new System.Drawing.Point(228, 41);
            this.textBoxDebug.Multiline = true;
            this.textBoxDebug.Name = "textBoxDebug";
            this.textBoxDebug.Size = new System.Drawing.Size(218, 369);
            this.textBoxDebug.TabIndex = 8;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(228, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 15);
            this.label1.TabIndex = 9;
            this.label1.Text = "Debug";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxDebug);
            this.Controls.Add(this.buttonNextStep);
            this.Controls.Add(this.textBoxScript);
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
        private System.Windows.Forms.RichTextBox textBoxScript;
        private System.Windows.Forms.Button buttonNextStep;
        private System.Windows.Forms.TextBox textBoxDebug;
        private System.Windows.Forms.Label label1;
    }
}

