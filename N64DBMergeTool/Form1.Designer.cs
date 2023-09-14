namespace N64DBMergeTool
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
            button1 = new Button();
            label1 = new Label();
            label2 = new Label();
            lbDataBase = new Label();
            label4 = new Label();
            lbAddtions = new Label();
            SuspendLayout();
            // 
            // button1
            // 
            button1.Location = new Point(12, 52);
            button1.Name = "button1";
            button1.Size = new Size(75, 23);
            button1.TabIndex = 0;
            button1.Text = "Open Files";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 34);
            label1.Name = "label1";
            label1.Size = new Size(518, 15);
            label1.TabIndex = 1;
            label1.Text = "Select an existing DB file and your addition source. A backup will be created of your database file.";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 88);
            label2.Name = "label2";
            label2.Size = new Size(79, 15);
            label2.TabIndex = 2;
            label2.Text = "Database File:";
            // 
            // lbDataBase
            // 
            lbDataBase.AutoSize = true;
            lbDataBase.Location = new Point(12, 113);
            lbDataBase.Name = "lbDataBase";
            lbDataBase.Size = new Size(0, 15);
            lbDataBase.TabIndex = 3;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 141);
            label4.Name = "label4";
            label4.Size = new Size(82, 15);
            label4.TabIndex = 4;
            label4.Text = "Additions File:";
            // 
            // lbAddtions
            // 
            lbAddtions.AutoSize = true;
            lbAddtions.Location = new Point(12, 168);
            lbAddtions.Name = "lbAddtions";
            lbAddtions.Size = new Size(0, 15);
            lbAddtions.TabIndex = 5;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 195);
            Controls.Add(lbAddtions);
            Controls.Add(label4);
            Controls.Add(lbDataBase);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(button1);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button button1;
        private Label label1;
        private Label label2;
        private Label lbDataBase;
        private Label label4;
        private Label lbAddtions;
    }
}