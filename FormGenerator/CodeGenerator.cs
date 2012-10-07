using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.IO;
using System.Diagnostics;

namespace FormGenerator
{
    public partial class CodeGenerator : Form
    {
        #region Declr
        string _form = "{0}"
            //Control Buttons
                        + "\n<div class=\"buttons\">"
                        + "\n<button type=\"submit\" class=\"button\">Save</button>"
                        + "\n<button type=\"button\" class=\"button white\">Cancel</button>"
                        + "\n</div>";

        string _dlddCode = "\n<div class=\"control-group\">"
                           + "\n<label class=\"control-label\" for=\"{field}\">"
                           + "\n{field}"
                           + "\n</label>"
                           + "\n<div class=\"controls\">"
                           + "\n{control}" //Control
                           + "\n<p class=\"help-block\">{field}</p>"
                           + "\n</div></div>";

        string _textInput = "<input type=\"text\" id=\"{0}\" req=\"true\" valsection=\"main\" valdata=\"{1}\" name=\"{0}\" class=\"medium\" />";
        string _hiddenInput = "<input type=\"hidden\" id=\"{0}\" name=\"{0}\" />";
        string _hiddenIDInput = "<input type=\"hidden\" value=\"0\" id=\"ID\" name=\"ID\" />";
        string _hiddenTrashInput = "<input type=\"hidden\" value=\"0\" id=\"trasht\" name=\"trasht\" />";
        string _selectSnippet = "<input type=\"hidden\" id=\"{0}\" name=\"{0}\" />";

        string _storedProc = @"
                            CREATE PROCEDURE {spname}
	                            (
                                    {params}
	                            )
                            AS
                            BEGIN
	                            SET NOCOUNT ON;
                                {body}
                            END";

        string _insertCmd = @"INSERT INTO {table}
                                (
                                    {cols}
                                )
                             VALUES
                                (
                                    {vals}
	                            )";
        string _updateCmd = "Update {table} SET {fields} WHERE ID = @ID";
        string _selectCmd = "SELECT {fields} FROM {table}";

        DataSet _dataSet = null;
        #endregion Declr

        List<string> ExcludeFields = new List<string>
        {
            "ID"
            ,"IsActive"
            ,"IsArchived"
            ,"CreatedBy"
            ,"CreatedOn"
            ,"ModifiedBy"
            ,"ModifiedOn"
        };

        public CodeGenerator()
        {
            InitializeComponent();
            txtCnString.Focus();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
            richTextBox1.Cut();
            {
                string _params = GetSPParams(_dataSet.Tables["Schema"]);
                richTextBox1.AppendText(
                    _storedProc
                    .Replace("{spname}", "Manage" + cmbTable.SelectedItem)
                    .Replace("{params}", _params)
                    .Replace("{body}", GetManageBody(_dataSet.Tables["Schema"])
                    )
                   );
                richTextBox1.AppendText("\nGo\n");
                richTextBox1.AppendText("\n--===================================================================================\n");
                richTextBox1.AppendText(
                    _storedProc
                    .Replace("{spname}", "Read" + cmbTable.SelectedItem)
                    .Replace("{params}", "@ID   int")
                    .Replace("{body}", GetReadBody(_dataSet.Tables["Schema"])
                    )
                   );
            }
            richTextBox1.SelectAll();
            richTextBox1.Copy();
            MessageBox.Show("Code Copied to your clipboard", "Copied!");
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            
        }

        private void cmbDatabase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbDatabase.SelectedIndex >= 0 && cmbDatabase.SelectedItem != null && !string.IsNullOrEmpty(cmbDatabase.SelectedItem.ToString()))
            {
                cmbTable.Enabled = true;
                using (SqlConnection _cn = new SqlConnection(txtCnString.Text))
                {
                    _cn.Open();
                    _cn.ChangeDatabase(cmbDatabase.SelectedItem.ToString());
                    using (SqlCommand _cmd = new SqlCommand("SELECT * FROM [" + cmbDatabase.SelectedItem.ToString() + "].SYS.TABLES", _cn))
                    {
                        _cmd.CommandType = System.Data.CommandType.Text;
                        SqlDataReader _reader = _cmd.ExecuteReader();
                        if (_reader != null)
                        {
                            cmbTable.Items.Clear();
                            while (_reader.Read())
                            {
                                cmbTable.Items.Add(_reader[0]);
                            }
                        }
                    }
                }
            }
            else
            {
                cmbTable.Enabled = false;
            }
        }

        #region Private Methods
        private string GetSPParams(DataTable _table)
        {
            string _params = string.Empty;
            foreach (DataRow _row in _table.Rows)
            {
                string _typeLen = "(" + _row["Length"].ToString() + ")";
                string _type = _row["Type"].ToString();

                if (_typeLen == "(-1)")
                {
                    _typeLen = "(MAX)";
                }
                if (_type == "int" || _type == "datetime" || _type == "bit")
                {
                    _typeLen = string.Empty;
                }
                if (_row["Column_name"].ToString().Equals("ID"))
                {
                    _params += "\t\t\t\t@" + _row["Column_name"] + "    " + _type + _typeLen + "\n";
                }
                else
                {
                    _params += "\t\t\t\t,@" + _row["Column_name"] + "    " + _type + _typeLen + "\n";
                }
            }
            return _params;
        }

        public string GetManageBody(DataTable _table)
        {
            string _manage = string.Empty;
            _manage += "IF @ID = 0\n";
            _manage += "BEGIN\n";
            string _cols = string.Empty;
            string _params = string.Empty;
            string _updateFields = string.Empty;
            int _colStart = 0;

            foreach (DataRow _row in _table.Rows)
            {
                if (!_row["Column_name"].ToString().Equals("ID"))
                {
                    if (_colStart == 0)
                    {
                        _cols += "\n\t\t\t\t" + _row["Column_name"];
                        _params += "\n\t\t\t\t@" + _row["Column_name"];
                        _colStart++;
                    }
                    else
                    {
                        _cols += "\n\t\t\t\t," + _row["Column_name"];
                        _params += "\n\t\t\t\t,@" + _row["Column_name"];
                    }
                    _updateFields += "," + _row["Column_name"] + " = @" + _row["Column_name"];
                }
            }
            _manage += _insertCmd.Replace("{table}", cmbTable.SelectedItem.ToString()).Replace("{cols}", _cols.TrimStart(',')).Replace("{vals}", _params.TrimStart(','));
            _manage += "\nEND\n";

            _manage += "\nELSE\n";
            _manage += "BEGIN\n";
            _manage += _updateCmd.Replace("{table}", cmbTable.SelectedItem.ToString()).Replace("{fields}", _updateFields.TrimStart(','));
            _manage += "\nEND\n";
            return _manage;
        }

        private string GetReadBody(DataTable _table)
        {
            string _read = string.Empty;
            string _fields = string.Empty;
            foreach (DataRow _row in _table.Rows)
            {
                _fields += "," + _row["Column_name"] + "\n";
            }
            _read += "IF @ID = 0\n";
            _read += "BEGIN\n";
            _read += _selectCmd.Replace("{table}", cmbTable.SelectedItem.ToString()).Replace("{fields}", _fields.TrimStart(','));
            _read += "\nEND\n";
            _read += "\nELSE\n";
            _read += "BEGIN\n";
            _read += _selectCmd.Replace("{table}", cmbTable.SelectedItem.ToString()).Replace("{fields}", _fields.TrimStart(',')) + " WHERE ID = @ID";
            _read += "\nEND\n";
            return _read;
        }

        private string FormCode(DataTable _table)
        {
            string _code = string.Empty;
            int _rowCount = 0;
            foreach (DataRow _row in _table.Rows)
            {
                string _columnName = _row["Column_name"].ToString();
                if (!ExcludeFields.Exists(s => s == _columnName))
                {
                    string _type = _row["Type"].ToString();
                    string _valData = _type.Equals("int") ? "num" : string.Empty;

                    string _input = _textInput.Replace("{0}", _columnName).Replace("{1}", _valData);
                    string _fields = _dlddCode.Replace("{field}", _columnName).Replace("{control}", _input);
                    if (_rowCount % 2 == 0)
                    {
                        _code += " " + _fields;
                    }
                    else
                    {
                        _code += _fields + " ";
                    }

                    _rowCount++;
                }
            }
            return _code;
        }
        #endregion

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox1.SelectAll();
            richTextBox1.Cut();
            string _params = "public class " + cmbTable.Text + " : BaseEntity{\n";
            foreach (DataRow _row in _dataSet.Tables["Schema"].Rows)
            {
                string _columnName = _row["Column_name"].ToString();
                if (!ExcludeFields.Exists(s => s == _columnName))
                {
                    string _attributes = "\n[AllowInsert, AllowUpdate]\n";
                    string _type = _row["Type"].ToString();
                    string _codeType = string.Empty;
                    switch (_type)
                    {
                        case "varchar":
                        case "nvarchar":
                            {
                                _codeType = "string";
                                break;
                            }
                        case "bit":
                            {
                                _codeType = "bool";
                                break;
                            }
                        case "datetime":
                            {
                                _codeType = "DateTime";
                                break;
                            }
                        default:
                            {
                                _codeType = _type;
                                break;
                            }
                    }

                    /*if (_type == "int" || _type == "datetime" || _type == "bit")
                    {
                        _typeLen = string.Empty;
                    }*/

                    _params += _attributes + "public " + _codeType + " " + _columnName + "{get;set;}\n";
                }
            }
            richTextBox1.AppendText(_params + "}");
        }

        private void txtCnString_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return)
            {
                LoadDBs();
            }
        }

        private void LoadDBs()
        {
            if (!string.IsNullOrEmpty(txtCnString.Text.Trim()))
            {
                cmbDatabase.Enabled = true;
                using (SqlConnection _cn = new SqlConnection(txtCnString.Text))
                {
                    _cn.Open();
                    using (SqlCommand _cmd = new SqlCommand("SELECT * FROM SYS.Databases", _cn))
                    {
                        _cmd.CommandType = System.Data.CommandType.Text;
                        SqlDataReader _reader = _cmd.ExecuteReader();
                        if (_reader != null)
                        {
                            cmbDatabase.Items.Clear();
                            while (_reader.Read())
                            {
                                cmbDatabase.Items.Add(_reader[0]);
                            }
                        }
                    }
                }
                cmbDatabase.Focus();
            }
            else
            {
                MessageBox.Show("Please enter valid connection string", "Errore!");
                cmbDatabase.Enabled = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cmbTable_SelectedIndexChanged(object sender, EventArgs e)
        {
            using (SqlConnection _cn = new SqlConnection(txtCnString.Text))
            {
                _cn.Open();
                _cn.ChangeDatabase(cmbDatabase.SelectedItem.ToString());
                using (SqlCommand _cmd = new SqlCommand("SP_HELP", _cn))
                {
                    _cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    SqlParameter _param = new SqlParameter("@objname", cmbTable.SelectedItem.ToString());
                    _cmd.Parameters.Add(_param);
                    _dataSet = new DataSet();
                    _dataSet.Load(_cmd.ExecuteReader(), LoadOption.OverwriteChanges, "Table", "Schema");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string _controls = FormCode(_dataSet.Tables["Schema"]);
            string _formCode = _form.Replace("{title}", "Manage " + cmbTable.SelectedItem.ToString())
                                    .Replace("{0}", _controls);
            richTextBox1.SelectAll();
            richTextBox1.Cut();
            richTextBox1.AppendText(_formCode);
            using (StreamWriter _writer = new StreamWriter("temp.html", false))
            {
                _writer.WriteLine(_formCode);
                Process.Start("temp.html");
            }
            richTextBox1.SelectAll();
            richTextBox1.Copy();
            MessageBox.Show("Code Copied to your clipboard", "Copied!");
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            LoadDBs();
        }
    }
}
