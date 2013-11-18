namespace WindowsFormsApplication1__gps_log_export
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.FromDateTime = new System.Windows.Forms.DateTimePicker();
            this.SearchBtn = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.ToDateTime = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.ExportBtn = new System.Windows.Forms.Button();
            this.number_label = new System.Windows.Forms.Label();
            this.resulttextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.numbertextBox = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.min_comboBox = new System.Windows.Forms.ComboBox();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.SuspendLayout();
            // 
            // FromDateTime
            // 
            this.FromDateTime.CustomFormat = "yyyy-MM-dd HH:mm";
            this.FromDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.FromDateTime.Location = new System.Drawing.Point(42, 10);
            this.FromDateTime.Name = "FromDateTime";
            this.FromDateTime.Size = new System.Drawing.Size(142, 22);
            this.FromDateTime.TabIndex = 0;
            this.FromDateTime.Value = new System.DateTime(2013, 11, 15, 0, 0, 0, 0);
            // 
            // SearchBtn
            // 
            this.SearchBtn.Location = new System.Drawing.Point(190, 12);
            this.SearchBtn.Name = "SearchBtn";
            this.SearchBtn.Size = new System.Drawing.Size(75, 23);
            this.SearchBtn.TabIndex = 1;
            this.SearchBtn.Text = "Search";
            this.SearchBtn.UseVisualStyleBackColor = true;
            this.SearchBtn.Click += new System.EventHandler(this.SearchBtn_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("PMingLiU", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(136)));
            this.label1.Location = new System.Drawing.Point(3, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(33, 12);
            this.label1.TabIndex = 2;
            this.label1.Text = "From:";
            // 
            // ToDateTime
            // 
            this.ToDateTime.CustomFormat = "yyyy-MM-dd HH:mm";
            this.ToDateTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.ToDateTime.Location = new System.Drawing.Point(42, 39);
            this.ToDateTime.Name = "ToDateTime";
            this.ToDateTime.Size = new System.Drawing.Size(142, 22);
            this.ToDateTime.TabIndex = 3;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("PMingLiU", 9F);
            this.label2.Location = new System.Drawing.Point(15, 41);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(21, 12);
            this.label2.TabIndex = 4;
            this.label2.Text = "To:";
            // 
            // ExportBtn
            // 
            this.ExportBtn.Location = new System.Drawing.Point(190, 40);
            this.ExportBtn.Name = "ExportBtn";
            this.ExportBtn.Size = new System.Drawing.Size(75, 23);
            this.ExportBtn.TabIndex = 5;
            this.ExportBtn.Text = "Export";
            this.ExportBtn.UseVisualStyleBackColor = true;
            this.ExportBtn.Click += new System.EventHandler(this.ExportBtn_Click);
            // 
            // number_label
            // 
            this.number_label.AutoSize = true;
            this.number_label.Location = new System.Drawing.Point(270, 16);
            this.number_label.Name = "number_label";
            this.number_label.Size = new System.Drawing.Size(32, 12);
            this.number_label.TabIndex = 0;
            this.number_label.Text = "數量:";
            this.number_label.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // resulttextBox
            // 
            this.resulttextBox.Location = new System.Drawing.Point(13, 76);
            this.resulttextBox.Multiline = true;
            this.resulttextBox.Name = "resulttextBox";
            this.resulttextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.resulttextBox.Size = new System.Drawing.Size(373, 264);
            this.resulttextBox.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(269, 44);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(32, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "誤差:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(363, 48);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 12);
            this.label4.TabIndex = 9;
            this.label4.Text = "分鐘";
            // 
            // numbertextBox
            // 
            this.numbertextBox.Location = new System.Drawing.Point(308, 12);
            this.numbertextBox.Name = "numbertextBox";
            this.numbertextBox.ReadOnly = true;
            this.numbertextBox.Size = new System.Drawing.Size(52, 22);
            this.numbertextBox.TabIndex = 10;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(366, 19);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(17, 12);
            this.label5.TabIndex = 11;
            this.label5.Text = "個";
            // 
            // min_comboBox
            // 
            this.min_comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.min_comboBox.FormattingEnabled = true;
            this.min_comboBox.Location = new System.Drawing.Point(308, 41);
            this.min_comboBox.Name = "min_comboBox";
            this.min_comboBox.Size = new System.Drawing.Size(52, 20);
            this.min_comboBox.TabIndex = 12;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(399, 352);
            this.Controls.Add(this.min_comboBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.numbertextBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.resulttextBox);
            this.Controls.Add(this.number_label);
            this.Controls.Add(this.ExportBtn);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.ToDateTime);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.SearchBtn);
            this.Controls.Add(this.FromDateTime);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker FromDateTime;
        private System.Windows.Forms.Button SearchBtn;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker ToDateTime;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button ExportBtn;
        private System.Windows.Forms.Label number_label;
        private System.Windows.Forms.TextBox resulttextBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox numbertextBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox min_comboBox;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
    }
}

