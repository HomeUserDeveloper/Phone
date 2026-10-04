namespace Phonebook;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Button btnAdd;
    private System.Windows.Forms.Button btnDelete;
    private System.Windows.Forms.Button btnEdit;
    private System.Windows.Forms.Button btnExport;
    private System.Windows.Forms.Button btnImportCsv;
    private System.Windows.Forms.Button btnExportVcf;
    private System.Windows.Forms.Button btnImportVcf;
    private System.Windows.Forms.Button btnPrint;
    private System.Windows.Forms.Button btnSelectAll;
    private System.Windows.Forms.Button btnClear;
    private System.Windows.Forms.TextBox txtSearch;
    private System.Windows.Forms.Button btnSearch;
    private System.Windows.Forms.Button btnRefresh;
    private System.Windows.Forms.DataGridView dgvContacts;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        var font = new System.Drawing.Font("Segoe UI", 10F);
        label5 = new Label();
        btnAdd = new Button();
        btnDelete = new Button();
        btnEdit = new Button();
        btnExport = new Button();
        btnImportCsv = new Button();
        btnExportVcf = new Button();
        btnImportVcf = new Button();
        btnPrint = new Button();
        btnSelectAll = new Button();
        btnClear = new Button();
        txtSearch = new TextBox();
        btnSearch = new Button();
        btnRefresh = new Button();
        dgvContacts = new DataGridView();

        SuspendLayout();

        AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = System.Drawing.Color.FromArgb(240, 245, 250);
        ClientSize = new System.Drawing.Size(1100, 650);
        MinimumSize = new System.Drawing.Size(900, 500);
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Телефонный справочник";

        label5.Text = "Поиск";
        label5.AutoSize = true;
        label5.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);

        txtSearch.Dock = DockStyle.Fill;
        txtSearch.Font = font;
        txtSearch.KeyDown += txtSearch_KeyDown;

        btnSearch.Text = "Найти";
        btnSearch.Dock = DockStyle.Fill;
        btnSearch.Font = font;
        btnSearch.Click += btnSearch_Click;

        btnClear.Text = "Очистить поиск";
        btnClear.Dock = DockStyle.Fill;
        btnClear.Font = font;
        btnClear.Click += btnClear_Click;

        var searchPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            ColumnCount = 3,
            Padding = new Padding(8),
            BackColor = BackColor
        };
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        searchPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        searchPanel.Controls.Add(label5, 0, 0);
        searchPanel.Controls.Add(txtSearch, 1, 0);
        searchPanel.Controls.Add(btnSearch, 2, 0);

        dgvContacts.Dock = DockStyle.Fill;
        dgvContacts.Font = font;
        dgvContacts.ReadOnly = false;
        dgvContacts.AllowUserToAddRows = false;
        dgvContacts.AllowUserToDeleteRows = false;
        dgvContacts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvContacts.RowHeadersVisible = false;
        dgvContacts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        dgvContacts.MultiSelect = true;
        dgvContacts.ColumnHeaderMouseClick += dgvContacts_ColumnHeaderMouseClick;
        dgvContacts.CurrentCellDirtyStateChanged += dgvContacts_CurrentCellDirtyStateChanged;
        dgvContacts.CellValueChanged += dgvContacts_CellValueChanged;

        btnAdd.Text = "Добавить";
        btnAdd.Click += btnAdd_Click;
        btnEdit.Text = "Редактировать";
        btnEdit.Click += btnEdit_Click;
        btnDelete.Text = "Удалить";
        btnDelete.Click += btnDelete_Click;
        btnSelectAll.Text = "Выделить всё";
        btnSelectAll.Click += btnSelectAll_Click;
        btnExport.Text = "Экспорт CSV";
        btnExport.Click += btnExport_Click;
        btnImportCsv.Text = "Импорт CSV";
        btnImportCsv.Click += btnImportCsv_Click;
        btnExportVcf.Text = "Экспорт VCF";
        btnExportVcf.Click += btnExportVcf_Click;
        btnImportVcf.Text = "Импорт VCF";
        btnImportVcf.Click += btnImportVcf_Click;
        btnPrint.Text = "Печать";
        btnPrint.Click += btnPrint_Click;
        btnRefresh.Text = "Обновить";
        btnRefresh.Click += btnRefresh_Click;

        var actionPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 175,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(10, 12, 10, 10),
            BackColor = BackColor
        };

        foreach (var button in new[] { btnAdd, btnEdit, btnDelete, btnSelectAll, btnExport, btnImportCsv, btnExportVcf, btnImportVcf, btnPrint, btnRefresh, btnClear })
        {
            button.Width = 150;
            button.Height = 38;
            button.Margin = new Padding(0, 0, 0, 10);
            button.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            actionPanel.Controls.Add(button);
        }

        ApplyButtonTheme(btnAdd, System.Drawing.Color.FromArgb(52, 152, 219), System.Drawing.Color.FromArgb(41, 128, 185), System.Drawing.Color.FromArgb(31, 97, 141));
        ApplyButtonTheme(btnEdit, System.Drawing.Color.FromArgb(142, 68, 173), System.Drawing.Color.FromArgb(103, 58, 183), System.Drawing.Color.FromArgb(72, 40, 128));
        ApplyButtonTheme(btnDelete, System.Drawing.Color.FromArgb(231, 76, 60), System.Drawing.Color.FromArgb(192, 57, 43), System.Drawing.Color.FromArgb(150, 40, 27));
        ApplyButtonTheme(btnSelectAll, System.Drawing.Color.FromArgb(46, 204, 113), System.Drawing.Color.FromArgb(39, 174, 96), System.Drawing.Color.FromArgb(28, 134, 72));
        ApplyButtonTheme(btnExport, System.Drawing.Color.FromArgb(26, 188, 156), System.Drawing.Color.FromArgb(22, 160, 133), System.Drawing.Color.FromArgb(15, 115, 96));
        ApplyButtonTheme(btnImportCsv, System.Drawing.Color.FromArgb(39, 174, 96), System.Drawing.Color.FromArgb(30, 132, 73), System.Drawing.Color.FromArgb(25, 111, 61));
        ApplyButtonTheme(btnExportVcf, System.Drawing.Color.FromArgb(155, 89, 182), System.Drawing.Color.FromArgb(123, 67, 151), System.Drawing.Color.FromArgb(94, 53, 115));
        ApplyButtonTheme(btnImportVcf, System.Drawing.Color.FromArgb(255, 153, 0), System.Drawing.Color.FromArgb(230, 126, 34), System.Drawing.Color.FromArgb(194, 101, 0));
        ApplyButtonTheme(btnPrint, System.Drawing.Color.FromArgb(243, 156, 18), System.Drawing.Color.FromArgb(211, 84, 0), System.Drawing.Color.FromArgb(180, 70, 0));
        ApplyButtonTheme(btnRefresh, System.Drawing.Color.FromArgb(91, 109, 246), System.Drawing.Color.FromArgb(72, 84, 196), System.Drawing.Color.FromArgb(54, 67, 156));
        ApplyButtonTheme(btnClear, System.Drawing.Color.FromArgb(149, 165, 166), System.Drawing.Color.FromArgb(127, 140, 141), System.Drawing.Color.FromArgb(96, 106, 106));
        ApplyButtonTheme(btnSearch, System.Drawing.Color.FromArgb(59, 130, 246), System.Drawing.Color.FromArgb(37, 99, 235), System.Drawing.Color.FromArgb(29, 78, 216));

        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(8)
        };
        contentPanel.Controls.Add(dgvContacts);
        contentPanel.Controls.Add(searchPanel);

        Controls.Add(contentPanel);
        Controls.Add(actionPanel);

        ResumeLayout(false);
    }
}
