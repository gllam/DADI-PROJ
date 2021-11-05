
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
            this.textBoxScript = new System.Windows.Forms.RichTextBox();
            this.buttonNextStep = new System.Windows.Forms.Button();
            this.textBoxDebug = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonRunAll = new System.Windows.Forms.Button();
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
            this.textBoxDebug.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
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
            // buttonRunAll
            // 
            this.buttonRunAll.Location = new System.Drawing.Point(452, 50);
            this.buttonRunAll.Name = "buttonRunAll";
            this.buttonRunAll.Size = new System.Drawing.Size(75, 23);
            this.buttonRunAll.TabIndex = 10;
            this.buttonRunAll.Text = "Run All";
            this.buttonRunAll.UseVisualStyleBackColor = true;
            this.buttonRunAll.Click += new System.EventHandler(this.ButtonRunAll_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.buttonRunAll);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxDebug);
            this.Controls.Add(this.buttonNextStep);
            this.Controls.Add(this.textBoxScript);
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
        private System.Windows.Forms.RichTextBox textBoxScript;
        private System.Windows.Forms.Button buttonNextStep;
        private System.Windows.Forms.TextBox textBoxDebug;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonRunAll;
    }
}

