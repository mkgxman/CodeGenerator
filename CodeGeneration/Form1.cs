using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Configuration;
using System.IO;

using LM.DataAccess;

namespace CodeGeneration
{
    public partial class Form1 : Form
    {
        public string cnnStr = ConfigurationManager.AppSettings["cnnStr"].ToString();
        private string filePath = ConfigurationManager.AppSettings["path"].ToString();

        private string KeyFieldName = "";

        StringBuilder QueryCtlDeclare = new StringBuilder("");
        StringBuilder QueryCtlDefine = new StringBuilder("");
        StringBuilder QueryCtlDetail = new StringBuilder("");

        StringBuilder EditCtlDeclare = new StringBuilder("");
        StringBuilder EditCtlDefine = new StringBuilder("");        
        StringBuilder EditCtlDetail = new StringBuilder("");

        StringBuilder btnDeclare = new StringBuilder("");//所有按钮共用
        StringBuilder btnDefine = new StringBuilder("");    //所有按钮共用
        StringBuilder btnDetail = new StringBuilder("");    //增、删、改、查、存、取消、关闭 按钮
        StringBuilder QbtnDetail = new StringBuilder("");   //查询按钮、重置按钮

        StringBuilder pageList = new StringBuilder("");
        StringBuilder pageDetail = new StringBuilder("");
        StringBuilder grpCondition = new StringBuilder("");
        StringBuilder btnPanel = new StringBuilder("");

        StringBuilder tabMain = new StringBuilder("");

        StringBuilder grpDetail = new StringBuilder("");


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            GetTableList();

            Init();
        }

        private void Init()
        {
            LocX.Text = ConfigurationManager.AppSettings["X"].ToString();
            LocY.Text = ConfigurationManager.AppSettings["Y"].ToString();
            FieldCount.Text = ConfigurationManager.AppSettings["Count"].ToString();
            DisY.Text = ConfigurationManager.AppSettings["disY"].ToString();
            DisX.Text = ConfigurationManager.AppSettings["disX"].ToString();
            
            foreach(string item in ConfigurationManager.AppSettings["FormNS"].ToString().Split(':'))
            {
                txtFrmNS.Items.Add(item); 
            }
            foreach (string item in ConfigurationManager.AppSettings["ClassNS"].ToString().Split(':'))
            {
                txtClassNS.Items.Add(item);
            }
        }

        private void GetTableList()
        {
            MsSql db = new MsSql(cnnStr);
            string sql = @"SELECT obj.Name AS TableName FROM sysobjects obj 
                            WHERE obj.xtype='U'
                            ORDER BY Name ";
            DataSet ds = new DataSet();
            ds = db.ExecuteDataSet(sql);

            if (ds != null)
            {
                cmbTableName.DataSource = ds.Tables[0];
                cmbTableName.DisplayMember = "TableName";
                cmbTableName.ValueMember = "TableName";
            }
        }

        private void GetField(string TableName)
        {
            MsSql db = new MsSql(cnnStr);
            string sql = @"SELECT col.name AS FieldName, col.name AS FieldText, 
                                    typ.name + '(' + cast(col.Length as varchar) + ')' AS FieldType,
                                    case col.isnullable when 1 then 0 when 0 then 1 end as  isnullable,
                                    1 as listcol, 0 as query, 1 as visiable, 1 as IsEditOrNot,'TextBox' as Ctl,
                                    0 as KeyField
                             FROM syscolumns col
                       INNER JOIN systypes typ ON typ.xtype=col.xtype
                       INNER JOIN sysobjects obj ON obj.ID=col.id
                            WHERE obj.Name='" + TableName + "' ORDER BY col.colid";
            DataSet ds = new DataSet();
            ds = db.ExecuteDataSet(sql);

            dataGridView1.AutoGenerateColumns = false;
            dataGridView1.DataSource = ds.Tables[0];
            dataGridView1.Columns[0].DataPropertyName = "FieldName";
            dataGridView1.Columns[1].DataPropertyName = "FieldText";
            dataGridView1.Columns[2].DataPropertyName = "FieldType";
            dataGridView1.Columns[4].DataPropertyName = "KeyField";
            dataGridView1.Columns[5].DataPropertyName = "listcol";
            dataGridView1.Columns[6].DataPropertyName = "query";
            dataGridView1.Columns[7].DataPropertyName = "isnullable";
            dataGridView1.Columns[8].DataPropertyName = "IsEditOrNot";
            dataGridView1.Columns[3].DataPropertyName = "ctl";
        }

        private void comboBox1_SelectionChangeCommitted(object sender, EventArgs e)
        {
            GetField(cmbTableName.SelectedValue.ToString());
        }

        private int GetPosX(int index)
        {
            return Convert.ToInt32(LocX.Text) + Convert.ToInt32(DisX.Text) * (index % Convert.ToInt32(FieldCount.Text));
        }
        private int GetPosY(int index)
        {
            return Convert.ToInt32(LocY.Text) + Convert.ToInt32(DisY.Text) * (index / Convert.ToInt32(FieldCount.Text));
        }

        private void GetKeyField()
        {
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(dr.Cells["PK"].Value) == true)
                    KeyFieldName = dr.Cells["FieldName"].Value.ToString();
            }
        }
        
        private void GetCtlInfo(DataGridViewRow dr, out string CtlName, out string FieldName, out int PropertyType)
        {
            string ControlName = "";

            CtlName = "";
            FieldName = "";
            PropertyType = -1;
            if (dr.Cells["ControlName"].Value == null || !Convert.ToBoolean(dr.Cells["IsEditOrNot"].Value))
                return;

            ControlName = dr.Cells["ControlName"].Value.ToString();
            FieldName = dr.Cells["FieldName"].Value.ToString();

            if (ControlName == "TextBox")
            {
                CtlName = "txt" + FieldName;
                PropertyType = 0;
            }
            else if (ControlName == "ComboBox")
            {
                CtlName = "cmb" + FieldName;
                PropertyType = 0;
            }
            else if (ControlName == "DateTimePicker")
            {
                CtlName = "dpt" + FieldName;
                PropertyType = 1;
            }
            else if (ControlName == "LMRefTextBox")
            {
                CtlName = "lmRefTxt" + FieldName;
                PropertyType = 2;
            }
            else if (ControlName == "LMComboBox")
            {
                CtlName = "lmCmb" + FieldName;
                PropertyType = 3;
            }
            else if (ControlName == "CheckBox")
            {
                CtlName = "chk" + FieldName;
                PropertyType = 4;
            }
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
            GetKeyField();

            //frm类：该文件tab页和非tab方式下代码绝大部分一致，因此共用一个函数，在有关的地方处理即可
            CreateFormCS();

            CreateButton();
            CreateControl();

            if (rbTab.Checked)
            {
                //frm.Designer类
                CreateFormDesignerTab();
            }
            else if (rbNotab.Checked)
            {
                //frm.Designer类
                CreateFormDesignerNoTab();
            }

            //数据库操作类
            CreateDBOperClass();

            MessageBox.Show("Done！", "", MessageBoxButtons.OK, MessageBoxIcon.Information);

        }

        //生成窗体文件
        private void CreateFormCS()
        {
            StringBuilder code = new StringBuilder("");

            FileStream fs1 = new FileStream(filePath + "frm" + cmbTableName.Text + ".cs", FileMode.Create, FileAccess.Write);//创建写入文件 
            StreamWriter sw = new StreamWriter(fs1);

            pageFrmCS.Text = "frm" + cmbTableName.Text + ".cs";

            #region 引用，类头

            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using System.ComponentModel;");
            code.AppendLine("using System.Data;");
            code.AppendLine("using System.Drawing;");
            code.AppendLine("using System.Text;");
            code.AppendLine("using System.Windows.Forms;");
            code.AppendLine("");
            code.AppendLine("using LM.ControlLib;");
            code.AppendLine("using LM.PublicLib.Framework;");
            code.AppendLine("using LM.PublicLib.PubFunc;");
            code.AppendLine("using MCO.Common;");

            code.AppendLine("");
            code.AppendLine("namespace " + txtFrmNS.Text);
            code.AppendLine("{");
            code.AppendLine("    public partial class frm" + cmbTableName.Text + ": Form");
            code.AppendLine("    {");
            code.AppendLine("        " + cmbTableName.Text + " " + cmbTableName.Text.ToLower() + " = new " + cmbTableName.Text + "(Config.cnnStr);");
            code.AppendLine("        DataSet ds" + cmbTableName.Text + " = new DataSet();");
            code.AppendLine("        BindingSource bindingSource1 = new BindingSource();");
            code.AppendLine("        //主键对应的变量，自动生成string类型，根据实现情况修改");
            code.AppendLine("        string " + KeyFieldName + " = \"\";");
            code.AppendLine("");
            code.AppendLine("        bool Add = false, Modify = false;");
            code.AppendLine("");
            code.AppendLine("        public frm" + cmbTableName.Text + "()");
            code.AppendLine("        {");
            code.AppendLine("            InitializeComponent();");
            code.AppendLine("            bindingSource1.BindingComplete += new System.Windows.Forms.BindingCompleteEventHandler(this.bindingSource1_BindingComplete);");
            code.AppendLine("        }");

            #endregion

            #region 数据变化时及时更新Grid里显示的数据
		    code.AppendLine("        private void bindingSource1_BindingComplete(object sender, BindingCompleteEventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            lmdgv" + cmbTableName.Text + "List.Invalidate();");
            code.AppendLine("        }");
	        #endregion
            

            #region Form_Resize

            code.AppendLine("");
            code.AppendLine("        public void frm" + cmbTableName.Text + "_Resize(object sender, EventArgs e)");
            code.AppendLine("        {");
            if(rbTab.Checked)
                code.AppendLine("            CommonFunc.SetFormat(this, tabMain, btnPanel);");
            if(rbNotab.Checked)
                code.AppendLine("            CommonFunc.SetFormat(this, grpCondition, grpDetail, lmdgv"+cmbTableName.Text+"List, btnPanel);");
            code.AppendLine("        }");

            #endregion

            #region Form_Closing

            code.AppendLine("");
            code.AppendLine("        public void frm" + cmbTableName.Text + "_FormClosing(object sender, FormClosingEventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            if (Add || Modify)");
            code.AppendLine("            {");
            code.AppendLine("                if (CommonFunc.ShowQuestionMsg(\"还有未保存的数据，要放弃修改吗？\") == DialogResult.No)");
            code.AppendLine("                    e.Cancel = true;");
            code.AppendLine("                else");
            code.AppendLine("                    e.Cancel = false;");
            code.AppendLine("            }");
            code.AppendLine("        }");

            #endregion

            #region Form_Load

            code.AppendLine("");
            code.AppendLine("        public void frm" + cmbTableName.Text + "_Load(object sender, EventArgs e)");
            code.AppendLine("        {");
            if (rbTab.Checked)
                code.AppendLine("            CommonFunc.SetFormat(this, tabMain, btnPanel);");
            if (rbNotab.Checked)
                code.AppendLine("            CommonFunc.SetFormat(this, grpCondition, grpDetail, lmdgv" + cmbTableName.Text + "List, btnPanel);");
            code.AppendLine("            CommonFunc.SetPower(btnPanel,Loginer.userPower);");
            code.AppendLine("");
            code.AppendLine("            //下面初始化函数，请根据实际需要保留或删除");
            code.AppendLine("            initQueryCtl();");
            code.AppendLine("            initDataGridView();");
            code.AppendLine("            initRefTextBox();");
            code.AppendLine("");
            if(rbTab.Checked)
                code.AppendLine("            CommonFunc.initUDF(\"" + cmbTableName.Text + "\", pageDetail.Controls, lmdgv" + cmbTableName.Text + "List);");
            else if(rbNotab.Checked)
                code.AppendLine("            CommonFunc.initUDF(\"" + cmbTableName.Text + "\", grpDetail.Controls, lmdgv" + cmbTableName.Text + "List);");
            code.AppendLine("            initDataSet();");
            code.AppendLine("            ControlDataBinding();");
            code.AppendLine("            SetEditStatus(false);");
            code.AppendLine("            //FillComboBox();");
            code.AppendLine("        }");

            #endregion


            int order = 0;
            string DataType = "";
            string CtlType = "";
            string CtlTypeCode = "";
            string QueryCtl = "";

            string FieldName = "";
            string FieldTitle = "";

            #region 初始化查询控件

            code.AppendLine("");
            code.AppendLine("        //根据实际需要调整查询控件的比较操作符、连接符、字段前缀等");
            code.AppendLine("        //第2段：在查询条件中的位置；第3段：C－字符；N－数字；D－日期；最后一段：1-TextBox；2－Combobox；3－DatetimePicker");
            code.AppendLine("        private void initQueryCtl()");
            code.AppendLine("        {");
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                QueryCtl = "";
                if (Convert.ToBoolean(dr.Cells["IsQuery"].Value))
                {
                    DataType = dr.Cells["FieldType"].Value.ToString().Substring(0, dr.Cells["FieldType"].Value.ToString().IndexOf('('));
                    FieldName = dr.Cells["FieldName"].Value.ToString();

                    if (dr.Cells["ControlName"].Value != null)
                        CtlType = dr.Cells["ControlName"].Value.ToString();
                    else
                        CtlType = "";

                    if (CtlType == "TextBox")
                    {
                        QueryCtl = "txtQ" + FieldName;
                        CtlTypeCode = "1";
                    }
                    else if (CtlType == "ComboBox")
                    {
                        QueryCtl = "cmbQ" + FieldName;
                        CtlTypeCode = "2";
                    }
                    else if (CtlType == "DateTimePicker")
                    {
                        QueryCtl = "dptQ" + FieldName;
                        CtlTypeCode = "3";
                    }
                    else if (CtlType == "LMRefTextBox")
                    {
                        QueryCtl = "lmRefTxtQ" + FieldName;
                        CtlTypeCode = "4";
                    }

                    if (DataType.IndexOf("char") != -1)
                        DataType = "C";
                    else if (DataType.IndexOf("datetime") != -1)
                        DataType = "D";
                    else if (DataType.IndexOf("int") != -1 || DataType.IndexOf("decimal") != -1 || DataType.IndexOf("money") != -1)
                        DataType = "N";
                    else
                        DataType = "XXXXX";
                }
                if (QueryCtl != "")
                {
                    code.AppendLine("            " + QueryCtl + ".Tag = \"Q;" + order.ToString() + ";" + DataType + ";AND " + dr.Cells["FieldName"].Value.ToString() + ";=;" + CtlTypeCode + "\";");
                    order++;
                }
            }
            code.AppendLine("        }");
            #endregion

            #region 初始化自定义参照控件

            code.AppendLine("");
            code.AppendLine("        private void initRefTextBox()");
            code.AppendLine("        {");
            code.AppendLine("            //将下面控件属性值根据实际情况补充完整");
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                if (dr.Cells["ControlName"].Value != null && dr.Cells["ControlName"].Value.ToString() == "LMRefTextBox")
                {
                    FieldName = dr.Cells["FieldName"].Value.ToString();
                    code.AppendLine("            lmRefTxt" + FieldName + ".TableName = \"\";");
                    code.AppendLine("            lmRefTxt" + FieldName + ".CodeFieldName = \"\";");
                    code.AppendLine("            lmRefTxt" + FieldName + ".TextFieldName = \"\";");
                    code.AppendLine("            lmRefTxt" + FieldName + ".HeadTitle = \"\";");
                    code.AppendLine("            lmRefTxt" + FieldName + ".FormTitle = \"\";");
                    if (Convert.ToBoolean(dr.Cells["IsQuery"].Value))
                    {
                        code.AppendLine("");
                        code.AppendLine("            lmRefTxtQ" + FieldName + ".TableName = \"\";");
                        code.AppendLine("            lmRefTxtQ" + FieldName + ".CodeFieldName = \"\";");
                        code.AppendLine("            lmRefTxtQ" + FieldName + ".TextFieldName = \"\";");
                        code.AppendLine("            lmRefTxtQ" + FieldName + ".HeadTitle = \"\";");
                        code.AppendLine("            lmRefTxtQ" + FieldName + ".FormTitle = \"\";");
                    }
                }
            }
            code.AppendLine("        }");

            #endregion

            #region 初始化DataGridView

            code.AppendLine("");
            code.AppendLine("        //根据需要修改各列的可见属性、宽度属性");
            code.AppendLine("        private void initDataGridView()");
            code.AppendLine("        {");
            code.AppendLine("            List<string> dgvCol = new List<string>();");
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                FieldName = dr.Cells["FieldName"].Value.ToString();
                FieldTitle = dr.Cells["FieldLabel"].Value.ToString();
                if (Convert.ToBoolean(dr.Cells["IsListCol"].Value))
                {
                    code.AppendLine("            dgvCol.Add(\"" + FieldTitle + ";" + FieldName + ";80;True;False\");");
                    if (FieldName.IndexOf("UDF") != -1)
                        code.AppendLine("            dgvCol.Add(\"" + FieldTitle + "Desc;" + FieldName + "Desc;80;True;False\");");
                }
            }
            code.AppendLine("            lmdgv" + cmbTableName.Text + "List.AutoGenerateColumns = false;");
            code.AppendLine("            lmdgv" + cmbTableName.Text + "List.ColumnDesc = dgvCol;");
            code.AppendLine("            lmdgv" + cmbTableName.Text + "List.SetColumns();");
            code.AppendLine("        }");

            #endregion

            #region 绑定数据到控件

            string CtlName = "";
            int PropertyType = 0;

            code.AppendLine("");
            code.AppendLine("        //控件和数据库的绑定，根据实际情况调试修改语句");
            code.AppendLine("        private void ControlDataBinding()");
            code.AppendLine("        {");
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                GetCtlInfo(dr, out CtlName, out FieldName, out PropertyType);
                if (CtlName == "")
                    continue;

                if (PropertyType == 0)  //绑定Text
                {
                    code.AppendLine("            " + CtlName + ".DataBindings.Add(\"Text\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, \"\");");
                }
                else if (PropertyType == 1) //时间控件，绑定Value
                {
                    code.AppendLine("            " + CtlName + ".DataBindings.Add(\"Value\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, null);");
                }
                else if (PropertyType == 2) //lmreftextbox
                {
                    code.AppendLine("            " + CtlName + ".innerTextBox.DataBindings.Add(\"Text\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, \"\");");
                    code.AppendLine("            " + CtlName + ".DataBindings.Add(\"Value\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, null);");
                }
                else if (PropertyType == 3) //lmComboBox
                {
                    code.AppendLine("            if (" + CtlName + ".DropDownStyle == ComboBoxStyle.Simple)");
                    code.AppendLine("                " + CtlName + ".DataBindings.Add(\"Text\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, \"\");");
                    code.AppendLine("            else");
                    code.AppendLine("            {");
                    code.AppendLine("                " + CtlName + ".DataBindings.Add(\"SelectedValue\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, \"\");");
                    code.AppendLine("                " + CtlName + ".DataBindings.Add(\"innerText\", bindingSource1, \"" + FieldName + "Desc\", true, DataSourceUpdateMode.OnPropertyChanged, \"\");");
                    code.AppendLine("            }");
                }
                else if (PropertyType == 4) //CheckBox
                {
                    code.AppendLine("            " + CtlName + ".DataBindings.Add(\"Checked\", bindingSource1, \"" + FieldName + "\", true, DataSourceUpdateMode.OnPropertyChanged, true);");

                }
            }
            code.AppendLine("        }");

            #endregion

            #region 获取主键值、单据号值等

            code.AppendLine("");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// 该函数的作用是在窗体加载时获取某些必要字段的值");
            code.AppendLine("        /// 根据实际信息修改下面的 XXXXX  为正确的字段的值");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        private void GetKeyValue()");
            code.AppendLine("        {");
            code.AppendLine("            " + KeyFieldName + " = ds" + cmbTableName.Text + ".Tables[0].Rows[bindingSource1.Position][\"" + KeyFieldName + "\"].ToString();");
            code.AppendLine("        }");

            #endregion

            #region 初始化记录集

            code.AppendLine("");
            code.AppendLine("        //初始化模块中用到的记录集的结构，用的是1＝2的查询条件，如果找到更合适的方法则替换");
            code.AppendLine("        private void initDataSet()");
            code.AppendLine("        {");
            code.AppendLine("            ds" + cmbTableName.Text + " = " + cmbTableName.Text.ToLower() + ".Get" + cmbTableName.Text + "List(\" and 1=2\");");
            code.AppendLine("            bindingSource1.DataSource = ds" + cmbTableName.Text + ".Tables[0];");
            code.AppendLine("            lmdgv" + cmbTableName.Text + "List.DataSource = bindingSource1;");
            code.AppendLine("        }");

            #endregion

            #region 开始编辑函数

            code.AppendLine("");
            code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
            code.AppendLine("        private void StartEdit()");
            code.AppendLine("        {");
            if (rbTab.Checked)
            {
                code.AppendLine("            tabMain.SelectedIndex = 1;");
                code.AppendLine("            pageDetail.Text = pageDetail.Text + \"*\";");
            }
            code.AppendLine("            SetEditStatus(true);");
            code.AppendLine("            txtXXXXXX.Focus();");
            code.AppendLine("        }");

            #endregion

            #region 结束编辑函数
            code.AppendLine("");
            code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
            code.AppendLine("        private void EndEdit()");
            code.AppendLine("        {");
            code.AppendLine("            Add = false;");
            code.AppendLine("            Modify = false;");
            if (rbTab.Checked)
            {
                code.AppendLine("            tabMain.SelectedIndex = 0;");
                code.AppendLine("            pageDetail.Text = pageDetail.Text.Substring(0, pageDetail.Text.Length - 1);");
            }
            code.AppendLine("            bindingSource1.EndEdit();");
            code.AppendLine("            SetEditStatus(false);");
            code.AppendLine("        }");
            #endregion

            #region 设置控件可编辑的函数
            code.AppendLine("");
            code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
            code.AppendLine("        private void SetEditStatus(bool value)");
            code.AppendLine("        {");
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                GetCtlInfo(dr, out CtlName, out FieldName, out PropertyType);
                if (CtlName != "")
                    code.AppendLine("            " + CtlName + ".Enabled = value;");
            }

            code.AppendLine("            lmbtnAdd.Enabled = !value;");
            code.AppendLine("            lmbtnDel.Enabled = !value;");
            code.AppendLine("            lmbtnModify.Enabled = !value;");
            code.AppendLine("            lmbtnSave.Enabled = value;");
            code.AppendLine("            lmbtnCancel.Enabled = value;");
            code.AppendLine("        }");
            #endregion

            #region DataGridView双击事件
            if (rbTab.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
                code.AppendLine("        private void lmdgv" + cmbTableName.Text + "List_MouseDoubleClick(object sender, MouseEventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            if (bindingSource1.Position == ds" + cmbTableName.Text + ".Tables[0].Rows.Count || ds" + cmbTableName.Text + ".Tables[0].Rows.Count ==0)");
                code.AppendLine("                return;");
                code.AppendLine("            GetKeyValue();");
                code.AppendLine("");
                code.AppendLine("            SetEditStatus(false);");
                code.AppendLine("            tabMain.SelectedIndex = 1;");
                code.AppendLine("        }");
            }
            #endregion

            #region tab页切换事件
            if (rbTab.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
                code.AppendLine("        private void tabMain_Selecting(object sender, TabControlCancelEventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            if (tabMain.SelectedIndex != 1 && (Add || Modify))");
                code.AppendLine("            {");
                code.AppendLine("                CommonFunc.ShowInforMsg(\"您所做的修改还未保存，请首先“保存”或“取消”后再转到其它页面\");");
                code.AppendLine("                e.Cancel = true;");
                code.AppendLine("            }");
                code.AppendLine("            if (tabMain.SelectedIndex == 1)");
                code.AppendLine("            {");
                code.AppendLine("                if (!Add)");
                code.AppendLine("                {");
                code.AppendLine("                    if (bindingSource1.Position == ds" + cmbTableName.Text + ".Tables[0].Rows.Count || ds" + cmbTableName.Text + ".Tables[0].Rows.Count == 0)");
                code.AppendLine("                        e.Cancel = true;");
                code.AppendLine("                    else");
                code.AppendLine("                    {");
                code.AppendLine("                        GetKeyValue();");
                code.AppendLine("                    }");
                code.AppendLine("                }");
                code.AppendLine("            }");
                code.AppendLine("        }");
            }
            #endregion

            #region 自定义参照调用函数
            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                GetCtlInfo(dr, out CtlName, out FieldName, out PropertyType);
                if (CtlName.IndexOf("lmRefTxt") != -1)
                {
                    code.AppendLine("");
                    code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
                    code.AppendLine("        public void " + CtlName + "_BtnClick(object sender, EventArgs e)");
                    code.AppendLine("        {");
                    code.AppendLine("            frmRef frm = new frmRef(ref " + CtlName + ");");
                    code.AppendLine("            frm.ShowDialog();");
                    code.AppendLine("        }");
                }
            }

            #endregion

            #region 查询
            code.AppendLine("");
            code.AppendLine("        private void lmbtnQuery_Click(object sender, EventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            ds" + cmbTableName.Text + " = " + cmbTableName.Text.ToLower() + ".Get" + cmbTableName.Text + "List(CommonFunc.GetCondition(grpCondition));");
            code.AppendLine("            bindingSource1.DataSource = ds" + cmbTableName.Text + ".Tables[0];");
            code.AppendLine("        }");
            #endregion

            #region 查询条件重置
            code.AppendLine("");
            code.AppendLine("        private void lmbtnReset_Click(object sender, EventArgs e)");
            code.AppendLine("        {");
            code.AppendLine("            CommonFunc.ResetQueryValue(grpCondition);");
            code.AppendLine("        }");
            #endregion

            #region 新增
            if (chkAdd.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        private void lmbtnAdd_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            bindingSource1.AddNew();");
                code.AppendLine("            Add = true;");
                code.AppendLine("            StartEdit();");
                code.AppendLine("        }");
            }
            #endregion

            #region 修改
            if (chkModify.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
                code.AppendLine("        private void lmbtnModify_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            if (bindingSource1.Position == ds" + cmbTableName.Text + ".Tables[0].Rows.Count || ds" + cmbTableName.Text + ".Tables[0].Rows.Count == 0)");
                code.AppendLine("            {");
                code.AppendLine("                CommonFunc.ShowInforMsg(\"请首先选择要修改的记录！\");");
                code.AppendLine("                return;");
                code.AppendLine("            }");
                code.AppendLine("            GetKeyValue();");
                code.AppendLine("");
                code.AppendLine("            Modify = true;");
                code.AppendLine("            StartEdit();");
                code.AppendLine("        }");
            }
            #endregion

            #region 删除
            if (chkDel.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        //需要根据对应模块的实际情况做修改调整");
                code.AppendLine("        private void lmbtnDel_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            if (ds" + cmbTableName.Text + ".Tables[0].Rows.Count == 0)");
                code.AppendLine("                return;");
                code.AppendLine("            //XXXXXX = ds" + cmbTableName.Text + ".Tables[0].Rows[bindingSource1.Position][\"XXXXXXX\"].ToString();");
                code.AppendLine("            //YYYYYYY.Text = ds" + cmbTableName.Text + ".Tables[0].Rows[bindingSource1.Position][\"YYYYYYYY\"].ToString();");
                code.AppendLine("");
                code.AppendLine("            if (" + KeyFieldName + ".Trim() == \"\")");
                code.AppendLine("            {");
                code.AppendLine("                CommonFunc.ShowInforMsg(\"请首先选择要删除的记录！\");");
                code.AppendLine("                return;");
                code.AppendLine("            }");
                code.AppendLine("            if (CommonFunc.ShowQuestionMsg(\"您确实要删除该记录吗？\") == DialogResult.No)");
                code.AppendLine("                return;");
                code.AppendLine("            //获取主键值");
                code.AppendLine("            " + cmbTableName.Text.ToLower() + "." + KeyFieldName + " = Convert.ToInt32(" + KeyFieldName + ");");
                code.AppendLine("            " + cmbTableName.Text.ToLower() + ".YYYYYYYYY = YYYYYYYYY.Text;");
                code.AppendLine("            if (" + cmbTableName.Text.ToLower() + ".Del() == -1)");
                code.AppendLine("            {");
                code.AppendLine("                CommonFunc.ShowErrMsg(\"删除失败！请稍候再试或联系管理员处理\");");
                code.AppendLine("            }");
                code.AppendLine("            else");
                code.AppendLine("            {");
                code.AppendLine("                CommonFunc.ShowInforMsg(\"删除成功！\");");
                code.AppendLine("                bindingSource1.RemoveCurrent();");
                code.AppendLine("            }");
                code.AppendLine("        }");
            }
            #endregion

            #region 保存
            if (chkSave.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        private void lmbtnSave_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                if (rbTab.Checked)
                    code.AppendLine("            if(!CommonFunc.CheckData(pageDetail.Controls))");
                if(rbNotab.Checked)
                    code.AppendLine("            if(!CommonFunc.CheckData(grpDetail.Controls))");
                code.AppendLine("                return;");
                code.AppendLine("            int ret = 0;");
                code.AppendLine("");
                code.AppendLine("            //此处跟主键相关的代码需要根据实际修改");
                code.AppendLine("            if(xxxxxx!= \"\")");
                code.AppendLine("                " + cmbTableName.Text.ToLower() + "." + KeyFieldName + " = Convert.ToInt32(" + KeyFieldName + ");");
                foreach (DataGridViewRow dr in dataGridView1.Rows)
                {
                    GetCtlInfo(dr, out CtlName, out FieldName, out PropertyType);
                    if (PropertyType == 0)  //text  123  Value   4  checked
                        code.AppendLine("            " + cmbTableName.Text.ToLower() + "." + FieldName + " = " + CtlName + ".Text;");
                    else if (PropertyType == 1 || PropertyType == 2)
                        code.AppendLine("            " + cmbTableName.Text.ToLower() + "." + FieldName + " = " + CtlName + ".Value;");
                    else if (PropertyType == 3)
                    {
                        code.AppendLine("            if (" + CtlName + ".DropDownStyle == ComboBoxStyle.Simple)");
                        code.AppendLine("                " + cmbTableName.Text.ToLower() + "." + FieldName + " = " + CtlName + ".Text;");
                        code.AppendLine("            else");
                        code.AppendLine("            {");
                        code.AppendLine("                if (" + CtlName + ".SelectedValue != null)");
                        code.AppendLine("                    " + cmbTableName.Text.ToLower() + "." + FieldName + " = " + CtlName + ".SelectedValue.ToString();");
                        code.AppendLine("            }");
                    }
                    else if (PropertyType == 4)
                        code.AppendLine("            " + cmbTableName.Text.ToLower() + "." + FieldName + " = " + CtlName + ".Checked?1:0;");
                }

                code.AppendLine("");
                code.AppendLine("            if (Add)");
                code.AppendLine("                ret = " + cmbTableName.Text.ToLower() + ".Add();");
                code.AppendLine("            else if (Modify)");
                code.AppendLine("                ret = " + cmbTableName.Text.ToLower() + ".Modify();");
                code.AppendLine("");
                code.AppendLine("            if (ret >= 0)");
                code.AppendLine("            {");
                code.AppendLine("                CommonFunc.ShowInforMsg(\"保存成功！\");");
                code.AppendLine("                EndEdit();");
                if (rbTab.Checked)
                {
                    code.AppendLine("                tabMain.SelectedIndex = 0;");
                }
                code.AppendLine("            }");
                code.AppendLine("            else if (ret == -2)");
                code.AppendLine("                CommonFunc.ShowErrMsg(\"数据重复！请修改角色名称\");");
                code.AppendLine("            else if (ret == -1)");
                code.AppendLine("                CommonFunc.ShowErrMsg(\"保存失败！请稍候再试\");");
                code.AppendLine("        }");
            }
            #endregion

            #region 取消
            if (chkCancel.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        private void lmbtnCancel_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            if (!Add && !Modify)");
                code.AppendLine("                return;");
                code.AppendLine("            if (CommonFunc.ShowQuestionMsg(\"您确实要取消变更吗？\") == DialogResult.Yes)");
                code.AppendLine("            {");
                code.AppendLine("                bindingSource1.CancelEdit();");
                code.AppendLine("                EndEdit();");
                code.AppendLine("            }");
                code.AppendLine("        }");
            }
            #endregion

            #region 打印
            if (chkPrint.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        private void lmbtnPrint_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("        ");
                code.AppendLine("        }");
            }
            #endregion

            #region 导出
            if (chkExport.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        private void lmbtnExport_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("        ");
                code.AppendLine("        }");
            }
            #endregion

            #region 关闭
            if (chkClose.Checked)
            {
                code.AppendLine("");
                code.AppendLine("        private void lmbtnClose_Click(object sender, EventArgs e)");
                code.AppendLine("        {");
                code.AppendLine("            this.Close();");
                code.AppendLine("        }");
            }
            #endregion

            code.AppendLine("  }");
            code.AppendLine("}");

            sw.Write(code.ToString());

            sw.Close();
            fs1.Close();

            txtFrmCS.Clear();
            txtFrmCS.AppendText(code.ToString());
        }

        //生成按钮 代码
        private void CreateButton()
        {
            btnDefine.Remove(0, btnDefine.Length);
            btnDetail.Remove(0, btnDetail.Length);
            btnPanel.Remove(0, btnPanel.Length);
            QbtnDetail.Remove(0, QbtnDetail.Length);
            grpCondition.Remove(0, grpCondition.Length);
            btnDeclare.Remove(0, btnDeclare.Length);

            int index = 1;

            if (chkClose.Checked)
            {
                GenerateBtnCode("lmbtnClose", "关闭(&C)", index, 1);
                index++;
            }
            if (chkPrint.Checked)
            {
                GenerateBtnCode("lmbtnPrint", "打印(&P)", index, 1);
                index++;
            }
            if (chkExport.Checked)
            {
                GenerateBtnCode("lmbtnExport", "导出(&E)", index, 1);
                index++;
            }
            if (chkCancel.Checked)
            {
                GenerateBtnCode("lmbtnCancel", " 取消(&S)", index, 1);
                index++;
            }
            if (chkSave.Checked)
            {
                GenerateBtnCode("lmbtnSave", "保存(&S)", index, 1);
                index++;
            }
            if (chkDel.Checked)
            {
                GenerateBtnCode("lmbtnDel", "删除(&D)", index, 1);
                index++;
            }
            if (chkModify.Checked)
            {
                GenerateBtnCode("lmbtnModify", "修改(&U)", index, 1);
                index++;
            }
            if (chkAdd.Checked)
            {
                GenerateBtnCode("lmbtnAdd", "新增(&A)", index, 1);
                index++;
            }

            if (chkQuery.Checked)
            {
                GenerateBtnCode("lmbtnQuery", "查询(&Q)", 1, 2);
            }
            if (chkReset.Checked)
            {
                GenerateBtnCode("lmbtnReset", "重置(&Q)", 2, 2);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="btnName">按钮名称</param>
        /// <param name="btnText">按钮标题</param>
        /// <param name="seq">按钮排序顺序</param>
        /// <param name="btnType">按钮类型：1－增删改查保存取消关闭；2－查询、重置</param>
        private void GenerateBtnCode(string btnName, string btnText, int seq, int btnType)
        {
            btnDefine.AppendLine("            this." + btnName + " = new LM.ControlLib.LMButton();");

            if (btnType == 1)
            {
                btnDetail.AppendLine("            //");
                btnDetail.AppendLine("            // " + btnName);
                btnDetail.AppendLine("            //");
                btnDetail.AppendLine("            this." + btnName + ".Name = \"" + btnName + "\";");
                btnDetail.AppendLine("            this." + btnName + ".FuncID = \"0\";");
                btnDetail.AppendLine("            this." + btnName + ".Location = new System.Drawing.Point(" + (20 + (8 - seq) * 100).ToString() + ", 20);");
                btnDetail.AppendLine("            this." + btnName + ".Size = new System.Drawing.Size(75, 23);");
                btnDetail.AppendLine("            this." + btnName + ".TabIndex = 8;");
                btnDetail.AppendLine("            this." + btnName + ".Seq = " + seq.ToString() + ";");
                btnDetail.AppendLine("            this." + btnName + ".Text = \"" + btnText + "\";");
                btnDetail.AppendLine("            this." + btnName + ".UseVisualStyleBackColor = true;");
                btnDetail.AppendLine("            this." + btnName + ".Click += new System.EventHandler(this." + btnName + "_Click);");

                btnPanel.AppendLine("            this.btnPanel.Controls.Add(this." + btnName + ");");
            }
            else if (btnType == 2)
            {
                QbtnDetail.AppendLine("            //");
                QbtnDetail.AppendLine("            // " + btnName);
                QbtnDetail.AppendLine("            //");
                QbtnDetail.AppendLine("            this." + btnName + ".Name = \"" + btnName + "\";");
                QbtnDetail.AppendLine("            this." + btnName + ".FuncID = \"0\";");
                QbtnDetail.AppendLine("            this." + btnName + ".Location = new System.Drawing.Point(" + (20 + (8 - seq) * 100).ToString() + ", 20);");
                QbtnDetail.AppendLine("            this." + btnName + ".Size = new System.Drawing.Size(75, 23);");
                QbtnDetail.AppendLine("            this." + btnName + ".TabIndex = 8;");
                QbtnDetail.AppendLine("            this." + btnName + ".Seq = " + seq.ToString() + ";");
                QbtnDetail.AppendLine("            this." + btnName + ".Text = \"" + btnText + "\";");
                QbtnDetail.AppendLine("            this." + btnName + ".UseVisualStyleBackColor = true;");
                QbtnDetail.AppendLine("            this." + btnName + ".Click += new System.EventHandler(this." + btnName + "_Click);");

                grpCondition.AppendLine("            this.grpCondition.Controls.Add(this." + btnName + ");");
            }
            btnDeclare.AppendLine("        private LM.ControlLib.LMButton " + btnName + ";");
        }
        
        //给四个保存代码的字符串赋值，并用于相关操作
        private void CreateControl()
        {
            EditCtlDefine.Remove(0, EditCtlDefine.Length);
            EditCtlDetail.Remove(0, EditCtlDetail.Length);
            EditCtlDeclare.Remove(0, EditCtlDeclare.Length);
            pageDetail.Remove(0, pageDetail.Length);
            grpDetail.Remove(0, grpDetail.Length);
            QueryCtlDefine.Remove(0, QueryCtlDefine.Length);
            QueryCtlDetail.Remove(0, QueryCtlDetail.Length);
            QueryCtlDeclare.Remove(0, QueryCtlDeclare.Length);
            //grpCondition.Remove(0, grpCondition.Length);

            int editindex = 0;
            int queryindex = 0;

            foreach (DataGridViewRow dr in dataGridView1.Rows)
            {
                if (dr.Cells["FieldLabel"].Value == null || dr.Cells["FieldLabel"].Value.ToString() == ""
                            || dr.Cells["ControlName"].Value == null || dr.Cells["ControlName"].Value.ToString() == "")
                    continue;

                if(!Convert.ToBoolean(dr.Cells["IsEditOrNot"].Value))
                    continue;

                GenerateControlCode(dr, editindex, queryindex);
                editindex++;
                queryindex++;
            }
        }

        private void GenerateControlCode(DataGridViewRow dr, int editorder, int queryorder)
        {
            
            #region
            string FieldName = "";
            string FieldLabel = "";
            string ControlName = "";
            
            bool IsQuery = false;
            bool IsEdit = false;
            bool IsNull = false;

            string labelPre = "";   //字段对应label控件的命名前缀
            string editPre = "";    //字段对应编辑框控件的命名前缀
            string labelCtlClass = "";  //标签对应的控件类
            string editCtlClass = "";   //编辑框对应的控件类
            string editName = "";   //编辑字段编辑框名
            string lblName = "";    //编辑字段标签名    
            string QeditName = "";  //查询字段编辑框名
            string QlblName = "";   //查询字段标签名

            FieldName = dr.Cells["FieldName"].Value.ToString();
            FieldLabel = dr.Cells["FieldLabel"].Value.ToString();
            if(dr.Cells["ControlName"].Value != null)
                ControlName = dr.Cells["ControlName"].Value.ToString();
            IsQuery = Convert.ToBoolean(dr.Cells["IsQuery"].Value);
            IsEdit = Convert.ToBoolean(dr.Cells["IsEditOrNot"].Value);
            IsNull = Convert.ToBoolean(dr.Cells["NotNull"].Value);
            #endregion

            #region 控件类型、命名前缀
            if (ControlName == "TextBox")
            {
                labelPre = "lbl";
                editPre = "txt";
                labelCtlClass = "System.Windows.Forms.Label";
                editCtlClass = "System.Windows.Forms.TextBox";
            }
            else if (ControlName == "LMComboBox")
            {
                labelPre = "lmLbl";
                editPre = "lmCmb";
                labelCtlClass = "LM.ControlLib.LMLabel";
                editCtlClass = "LM.ControlLib.LMComboBox";
            }
            else if (ControlName == "LMRefTextBox")
            {
                labelPre = "lbl";
                editPre = "lmRefTxt";
                labelCtlClass = "System.Windows.Forms.Label";
                editCtlClass = "LM.ControlLib.LMRefTextBox";
            }
            else if (ControlName == "ComboBox")
            {
                labelPre = "lbl";
                editPre = "cmb";
                labelCtlClass = "System.Windows.Forms.Label";
                editCtlClass = "System.Windows.Forms.ComboBox";
            }
            else if (ControlName == "DateTimePicker")
            {
                labelPre = "lbl";
                editPre = "dpt";
                labelCtlClass = "System.Windows.Forms.Label";
                editCtlClass = "System.Windows.Forms.DateTimePicker";
            }
            else if (ControlName == "CheckBox")
            {
                labelPre = "lbl";
                editPre = "chk";
                labelCtlClass = "System.Windows.Forms.Label";
                editCtlClass = "System.Windows.Forms.CheckBox";
            }
            #endregion

            if (IsEdit)
            {
                #region 数据编辑控件
                lblName = labelPre + FieldName;
                editName = editPre + FieldName;
                
                EditCtlDefine.AppendLine("            this." + lblName + " = new " + labelCtlClass + "();");
                EditCtlDefine.AppendLine("            this." + editName + " = new " + editCtlClass + "();");

                EditCtlDetail.AppendLine("            //");
                EditCtlDetail.AppendLine("            // " + lblName);
                EditCtlDetail.AppendLine("            //");
                EditCtlDetail.AppendLine("            this." + lblName + ".AutoSize = false;");
                EditCtlDetail.AppendLine("            this." + lblName + ".Width = 80;");
                EditCtlDetail.AppendLine("            this." + lblName + ".Name = \"" + lblName + "\";");
                if (labelPre == "lmLbl")
                    EditCtlDetail.AppendLine("            this." + lblName + ".FieldName = \"" + FieldName + "\";");
                if (IsNull)
                    EditCtlDetail.AppendLine("            this." + lblName + ".ForeColor = System.Drawing.Color.Red;");
                EditCtlDetail.AppendLine("            this." + lblName + ".Text = \"" + FieldLabel + "\";");
                EditCtlDetail.AppendLine("            this." + lblName + ".Location = new System.Drawing.Point(" + GetPosX(editorder).ToString() + "," + GetPosY(editorder).ToString() + ");");
                EditCtlDetail.AppendLine("            this." + lblName + ".TabIndex = " + Convert.ToString(100 + editorder) + ";");
                EditCtlDetail.AppendLine("            this." + lblName + ".TextAlign = System.Drawing.ContentAlignment.MiddleRight;");

                EditCtlDetail.AppendLine("            //");
                EditCtlDetail.AppendLine("            // " + editName);
                EditCtlDetail.AppendLine("            //");
                EditCtlDetail.AppendLine("            this." + editName + ".Name = \"" + editName + "\";");
                if (labelPre == "lmLbl")
                    EditCtlDetail.AppendLine("            this." + editName + ".FieldName = \"" + FieldName + "\";");
                if (editPre == "dpt")
                {
                    EditCtlDetail.AppendLine("            this." + editName + ".CustomFormat = \"yyyy-M-dd HH:mm:ss\";");
                    EditCtlDetail.AppendLine("            this." + editName + ".Format = System.Windows.Forms.DateTimePickerFormat.Custom;");
                }
                EditCtlDetail.AppendLine("            this." + editName + ".Location = new System.Drawing.Point(" + (GetPosX(editorder) + 82).ToString() + "," + GetPosY(editorder).ToString() + ");");
                EditCtlDetail.AppendLine("            this." + editName + ".TabIndex = " + editorder.ToString() + ";");
                if (IsNull)
                {
                    EditCtlDetail.AppendLine("            this." + editName + ".Tag = \"1;" + FieldLabel + "\";");
                }
                if(editPre=="lmRefTxt")
                    EditCtlDetail.AppendLine("            this." + editName + ".BtnClick += new LM.ControlLib.LMRefTextBox.BtnClickHandle(this." + editName + "_BtnClick);");

                EditCtlDeclare.AppendLine("        private " + labelCtlClass + " " + lblName + ";");
                EditCtlDeclare.AppendLine("        private " + editCtlClass + " " + editName + ";");

                //tab页格式和非tab格式的仅仅是下面两行不同，后续版本和重构该函数
                if (rbTab.Checked)
                {
                    pageDetail.AppendLine("            this.pageDetail.Controls.Add(this." + lblName + ");");
                    pageDetail.AppendLine("            this.pageDetail.Controls.Add(this." + editName + ");");
                }
                else if (rbNotab.Checked)
                {
                    grpDetail.AppendLine("            this.grpDetail.Controls.Add(this." + lblName + ");");
                    grpDetail.AppendLine("            this.grpDetail.Controls.Add(this." + editName + ");");
                }

                #endregion
            }
            if(IsQuery)
            {
                #region 查询控件
                QlblName = labelPre + "Q" + FieldName;
                QeditName = editPre + "Q" + FieldName;

                QueryCtlDefine.AppendLine("            this." + QlblName + " = new " + labelCtlClass + "();");
                QueryCtlDefine.AppendLine("            this." + QeditName + " = new " + editCtlClass + "();");

                QueryCtlDetail.AppendLine("            //");
                QueryCtlDetail.AppendLine("            // " + QlblName);
                QueryCtlDetail.AppendLine("            //");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".Name = \"" + QlblName + "\";");
                if (labelPre == "lmLbl")
                    QueryCtlDetail.AppendLine("            this." + QlblName + ".FieldName = \"" + FieldName + "\";");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".Text = \"" + FieldLabel + "\";");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".Location = new System.Drawing.Point(" + GetPosX(queryorder).ToString() + "," + GetPosY(queryorder).ToString() + ");");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".AutoSize = false;");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".Width = 80;");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".TabIndex = " + Convert.ToString(200 + queryorder) + ";");
                QueryCtlDetail.AppendLine("            this." + QlblName + ".TextAlign = System.Drawing.ContentAlignment.MiddleRight;");

                QueryCtlDetail.AppendLine("            //");
                QueryCtlDetail.AppendLine("            // " + QeditName);
                QueryCtlDetail.AppendLine("            //");
                QueryCtlDetail.AppendLine("            this." + QeditName + ".Name = \"" + QeditName + "\";");
                if (labelPre == "lmLbl")
                    QueryCtlDetail.AppendLine("            this." + QeditName + ".FieldName = \"" + FieldName + "\";");
                if (editPre == "dpt")
                {
                    QueryCtlDetail.AppendLine("            this." + editName + ".CustomFormat = \"yyyy-M-dd HH:mm:ss\";");
                    QueryCtlDetail.AppendLine("            this." + editName + ".Format = System.Windows.Forms.DateTimePickerFormat.Custom;");
                    QueryCtlDetail.AppendLine("            this." + editName + ".ShowCheckBox = true;");
                    QueryCtlDetail.AppendLine("            this." + editName + ".Checked = false;");
                }
                QueryCtlDetail.AppendLine("            this." + QeditName + ".Location = new System.Drawing.Point(" + (GetPosX(queryorder) + 82).ToString() + "," + GetPosY(queryorder).ToString() + ");");
                QueryCtlDetail.AppendLine("            this." + QeditName + ".TabIndex = " + queryorder.ToString() + ";");

                QueryCtlDeclare.AppendLine("        private " + labelCtlClass + " " + QlblName + ";");
                QueryCtlDeclare.AppendLine("        private " + editCtlClass + " " + QeditName + ";");

                grpCondition.AppendLine("            this.grpCondition.Controls.Add(this." + QlblName + ");");
                grpCondition.AppendLine("            this.grpCondition.Controls.Add(this." + QeditName + ");");
                #endregion
            }

        }

        #region 生成tab方式的界面设计类
        //生成窗体布局文件
        private void CreateFormDesignerTab()
        {
            StringBuilder code = new StringBuilder("");

            FileStream fs1 = new FileStream(filePath + "frm" + cmbTableName.Text + ".Designer.cs", FileMode.Create, FileAccess.Write);//创建写入文件 
            StreamWriter sw = new StreamWriter(fs1);

            code.AppendLine("namespace " + txtFrmNS.Text);
            code.AppendLine("{");
            code.AppendLine("    partial class frm" + cmbTableName.Text );
            code.AppendLine("    {");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Required designer variable.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        private System.ComponentModel.IContainer components = null;");
            code.AppendLine("");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Clean up any resources being used.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"disposing\">true if managed resources should be disposed; otherwise, false.</param>");
            code.AppendLine("        protected override void Dispose(bool disposing)");
            code.AppendLine("        {");
            code.AppendLine("            if (disposing && (components != null))");
            code.AppendLine("            {");
            code.AppendLine("                components.Dispose();");
            code.AppendLine("            }");
            code.AppendLine("            base.Dispose(disposing);");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        #region Windows Form Designer generated code");
            code.AppendLine("");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Required method for Designer support - do not modify");
            code.AppendLine("        /// the contents of this method with the code editor.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        private void InitializeComponent()");
            code.AppendLine("        {");

            code.AppendLine("            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();");
            code.AppendLine("            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frm"+cmbTableName.Text+"));");
            code.AppendLine("            this.tabMain = new System.Windows.Forms.TabControl();");
            code.AppendLine("            this.pageList = new System.Windows.Forms.TabPage();");
            code.AppendLine("            this.grpCondition = new System.Windows.Forms.GroupBox();");
            code.AppendLine(QueryCtlDefine.ToString());
            code.AppendLine("            this.pageDetail = new System.Windows.Forms.TabPage();");
            code.AppendLine(EditCtlDefine.ToString());
            code.AppendLine("            this.btnPanel = new System.Windows.Forms.Panel();");
            code.AppendLine(btnDefine.ToString());
            code.AppendLine("            this.lmdgv" +cmbTableName.Text+ "List = new LM.ControlLib.LMDataGridView();");
            code.AppendLine("            this.tabMain.SuspendLayout();");
            code.AppendLine("            this.pageList.SuspendLayout();");
            code.AppendLine("            this.grpCondition.SuspendLayout();");
            code.AppendLine("            this.pageDetail.SuspendLayout();");
            code.AppendLine("            this.btnPanel.SuspendLayout();");
            code.AppendLine("            ((System.ComponentModel.ISupportInitialize)(this.lmdgv"+cmbTableName.Text+"List)).BeginInit();");
            code.AppendLine("            this.SuspendLayout();");

            code.AppendLine("            //");
            code.AppendLine("            // tabMain");
            code.AppendLine("            // ");
            code.AppendLine("            this.tabMain.Controls.Add(this.pageList);");
            code.AppendLine("            this.tabMain.Controls.Add(this.pageDetail);");
            code.AppendLine("            this.tabMain.Dock = System.Windows.Forms.DockStyle.Top;");
            code.AppendLine("            this.tabMain.Location = new System.Drawing.Point(0, 0);");
            code.AppendLine("            this.tabMain.Name = \"tabMain\";");
            code.AppendLine("            this.tabMain.SelectedIndex = 0;");
            code.AppendLine("            this.tabMain.Size = new System.Drawing.Size(892, 512);");
            code.AppendLine("            this.tabMain.TabIndex = 0;");
            code.AppendLine("            this.tabMain.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.tabMain_Selecting);");
            code.AppendLine("            //");
            code.AppendLine("            // pageList");
            code.AppendLine("            // ");
            code.AppendLine("            this.pageList.Controls.Add(this.lmdgv"+cmbTableName.Text+"List);");
            code.AppendLine("            this.pageList.Controls.Add(this.grpCondition);");
            code.AppendLine("            this.pageList.Location = new System.Drawing.Point(4, 22);");
            code.AppendLine("            this.pageList.Name = \"pageList\";");
            code.AppendLine("            this.pageList.Padding = new System.Windows.Forms.Padding(3);");
            code.AppendLine("            this.pageList.Size = new System.Drawing.Size(884, 486);");
            code.AppendLine("            this.pageList.TabIndex = 0;");
            code.AppendLine("            this.pageList.Text = \"列表\";");
            code.AppendLine("            this.pageList.UseVisualStyleBackColor = true;");

            code.AppendLine("            //");
            code.AppendLine("            // grpCondition");
            code.AppendLine("            // ");
            code.AppendLine(grpCondition.ToString());
            code.AppendLine("            this.grpCondition.Dock = System.Windows.Forms.DockStyle.Top;");
            code.AppendLine("            this.grpCondition.Location = new System.Drawing.Point(3, 3);");
            code.AppendLine("            this.grpCondition.Name = \"grpCondition\";");
            code.AppendLine("            this.grpCondition.Size = new System.Drawing.Size(878, 115);");
            code.AppendLine("            this.grpCondition.TabIndex = 0;");
            code.AppendLine("            this.grpCondition.TabStop = false;");
            code.AppendLine("            this.grpCondition.Text = \"查询条件\";");
            code.AppendLine(QueryCtlDetail.ToString());
            code.AppendLine(QbtnDetail.ToString());

            code.AppendLine("            //");
            code.AppendLine("            // pageDetail");
            code.AppendLine("            // ");
            code.AppendLine(pageDetail.ToString());
            code.AppendLine("            this.pageDetail.Location = new System.Drawing.Point(4, 22);");
            code.AppendLine("            this.pageDetail.Name = \"pageDetail\";");
            code.AppendLine("            this.pageDetail.Padding = new System.Windows.Forms.Padding(3);");
            code.AppendLine("            this.pageDetail.Size = new System.Drawing.Size(884, 486);");
            code.AppendLine("            this.pageDetail.TabIndex = 1;");
            code.AppendLine("            this.pageDetail.Text = \"详细信息\";");
            code.AppendLine("            this.pageDetail.UseVisualStyleBackColor = true;");
            code.AppendLine(EditCtlDetail.ToString());

            code.AppendLine("            //");
            code.AppendLine("            // btnPanel");
            code.AppendLine("            //");
            code.AppendLine(btnPanel.ToString());
            code.AppendLine("            this.btnPanel.Dock = System.Windows.Forms.DockStyle.Bottom;");
            code.AppendLine("            this.btnPanel.Location = new System.Drawing.Point(0, 518);");
            code.AppendLine("            this.btnPanel.Name = \"btnPanel\";");
            code.AppendLine("            this.btnPanel.Size = new System.Drawing.Size(892, 55);");
            code.AppendLine("            this.btnPanel.TabIndex = 1;");
            code.AppendLine(btnDetail.ToString());

            code.AppendLine("            //");
            code.AppendLine("            // lmdgvUserList");
            code.AppendLine("            //");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.AllowUserToDeleteRows = false;");
            code.AppendLine("            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;");
            //code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.ColumnDesc = ((System.Collections.Generic.List<string>)(resources.GetObject(\"lmdgv" + cmbTableName.Text + "List.ColumnDesc\")));");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Dock = System.Windows.Forms.DockStyle.Fill;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Location = new System.Drawing.Point(3, 118);");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Name = \"lmdgv" + cmbTableName.Text + "List\";");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.ReadOnly = true;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.RowTemplate.Height = 23;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Size = new System.Drawing.Size(878, 365);");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.TabIndex = 1;");
            //if(rbTab.Checked)
            //    code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.lmdgv" + cmbTableName.Text + "List_MouseDoubleClick);");

            code.AppendLine("            //"); 
            code.AppendLine("            // frm" + cmbTableName.Text);
            code.AppendLine("            //"); 
            code.AppendLine("            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);");
            code.AppendLine("            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;");
            code.AppendLine("            this.ClientSize = new System.Drawing.Size(892, 573);");
            code.AppendLine("            this.Controls.Add(this.btnPanel);");
            code.AppendLine("            this.Controls.Add(this.tabMain);");
            code.AppendLine("            this.Name = \"frm" + cmbTableName.Text + "\";");
            code.AppendLine("            this.Text = \"" + txtFrmTitle.Text + "\";");
            code.AppendLine("            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;");
            code.AppendLine("            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frm" + cmbTableName.Text + "_FormClosing);");
            code.AppendLine("            this.Load += new System.EventHandler(this.frm" + cmbTableName.Text + "_Load);");
            code.AppendLine("            this.Resize += new System.EventHandler(this.frm" + cmbTableName.Text + "_Resize);");
            code.AppendLine("            this.tabMain.ResumeLayout(false);");
            code.AppendLine("            this.pageList.ResumeLayout(false);");
            code.AppendLine("            this.grpCondition.ResumeLayout(false);");
            code.AppendLine("            this.grpCondition.PerformLayout();");
            code.AppendLine("            this.pageDetail.ResumeLayout(false);");
            code.AppendLine("            this.pageDetail.PerformLayout();");
            code.AppendLine("            this.btnPanel.ResumeLayout(false);");
            code.AppendLine("            ((System.ComponentModel.ISupportInitialize)(this.lmdgv" + cmbTableName.Text + "List)).EndInit();");
            code.AppendLine("            this.ResumeLayout(false);");
            code.AppendLine("");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        #endregion");
            code.AppendLine("");
            code.AppendLine("        private System.Windows.Forms.TabControl tabMain;");
            code.AppendLine("        private System.Windows.Forms.TabPage pageList;");
            code.AppendLine("        private System.Windows.Forms.TabPage pageDetail;");
            code.AppendLine("        private System.Windows.Forms.GroupBox grpCondition;");
            code.AppendLine("        private System.Windows.Forms.Panel btnPanel;");
            code.AppendLine(EditCtlDeclare.ToString());
            code.AppendLine(QueryCtlDeclare.ToString());
            code.AppendLine("        private LM.ControlLib.LMDataGridView lmdgv" + cmbTableName.Text + "List;");
            code.AppendLine(btnDeclare.ToString());
            code.AppendLine("  }");
            code.AppendLine("}");

            sw.Write(code.ToString());

            sw.Close();
            fs1.Close();

            txtDesignerCS.Clear();
            txtDesignerCS.Text = code.ToString();
        }
        #endregion

        #region 生成非tab页方式的界面设计类
        //生成窗体布局文件
        private void CreateFormDesignerNoTab()
        {
            StringBuilder code = new StringBuilder("");

            FileStream fs1 = new FileStream(filePath + "frm" + cmbTableName.Text + ".Designer.cs", FileMode.Create, FileAccess.Write);//创建写入文件 
            StreamWriter sw = new StreamWriter(fs1);

            code.AppendLine("namespace " + txtFrmNS.Text);
            code.AppendLine("{");
            code.AppendLine("    partial class frm" + cmbTableName.Text);
            code.AppendLine("    {");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Required designer variable.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        private System.ComponentModel.IContainer components = null;");
            code.AppendLine("");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Clean up any resources being used.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        /// <param name=\"disposing\">true if managed resources should be disposed; otherwise, false.</param>");
            code.AppendLine("        protected override void Dispose(bool disposing)");
            code.AppendLine("        {");
            code.AppendLine("            if (disposing && (components != null))");
            code.AppendLine("            {");
            code.AppendLine("                components.Dispose();");
            code.AppendLine("            }");
            code.AppendLine("            base.Dispose(disposing);");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        #region Windows Form Designer generated code");
            code.AppendLine("");
            code.AppendLine("        /// <summary>");
            code.AppendLine("        /// Required method for Designer support - do not modify");
            code.AppendLine("        /// the contents of this method with the code editor.");
            code.AppendLine("        /// </summary>");
            code.AppendLine("        private void InitializeComponent()");
            code.AppendLine("        {");

            code.AppendLine("            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();");
            code.AppendLine("            this.btnPanel = new System.Windows.Forms.Panel();");
            code.AppendLine(btnDefine.ToString());
            code.AppendLine("            this.grpCondition = new System.Windows.Forms.GroupBox();");
            code.AppendLine(QueryCtlDefine.ToString());
            code.AppendLine("            this.grpDetail = new System.Windows.Forms.GroupBox();");
            code.AppendLine(EditCtlDefine.ToString());
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List = new LM.ControlLib.LMDataGridView();");
            code.AppendLine("            this.btnPanel.SuspendLayout();");
            code.AppendLine("            this.grpCondition.SuspendLayout();");
            code.AppendLine("            this.grpDetail.SuspendLayout();");
            code.AppendLine("            ((System.ComponentModel.ISupportInitialize)(this.lmdgv" + cmbTableName.Text + "List)).BeginInit();");
            code.AppendLine("            this.SuspendLayout();");

            code.AppendLine("            //");
            code.AppendLine("            // btnPanel");
            code.AppendLine("            //");
            code.AppendLine(btnPanel.ToString());
            code.AppendLine("            this.btnPanel.Dock = System.Windows.Forms.DockStyle.Bottom;");
            code.AppendLine("            this.btnPanel.Location = new System.Drawing.Point(0, 518);");
            code.AppendLine("            this.btnPanel.Name = \"btnPanel\";");
            code.AppendLine("            this.btnPanel.Size = new System.Drawing.Size(892, 55);");
            code.AppendLine("            this.btnPanel.TabIndex = 1;");
            code.AppendLine(btnDetail.ToString());

            code.AppendLine("            //");
            code.AppendLine("            // grpCondition");
            code.AppendLine("            // ");
            code.AppendLine(grpCondition.ToString());
            code.AppendLine("            this.grpCondition.Location = new System.Drawing.Point(0, 2);");
            code.AppendLine("            this.grpCondition.Name = \"grpCondition\";");
            code.AppendLine("            this.grpCondition.Size = new System.Drawing.Size(878, 80);");
            code.AppendLine("            this.grpCondition.TabIndex = 0;");
            code.AppendLine("            this.grpCondition.TabStop = false;");
            code.AppendLine("            this.grpCondition.Text = \"查询条件\";");
            code.AppendLine(QueryCtlDetail.ToString());
            code.AppendLine(QbtnDetail.ToString());

            code.AppendLine("            //");
            code.AppendLine("            // grpDetail");
            code.AppendLine("            // ");
            code.AppendLine(grpDetail.ToString());
            code.AppendLine("            this.grpDetail.Location = new System.Drawing.Point(0, 82);");
            code.AppendLine("            this.grpDetail.Name = \"grpDetail\";");
            code.AppendLine("            this.grpDetail.Size = new System.Drawing.Size(891, 200);");
            code.AppendLine("            this.grpDetail.TabIndex = 4;");
            code.AppendLine("            this.grpDetail.Text = \"详细信息\";");
            code.AppendLine("            this.grpDetail.TabStop = false;");
            code.AppendLine(EditCtlDetail.ToString());

            code.AppendLine("            //");
            code.AppendLine("            // lmdgvUserList");
            code.AppendLine("            //");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.AllowUserToDeleteRows = false;");
            code.AppendLine("            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(255)))), ((int)(((byte)(192)))));");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;");
            //code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.ColumnDesc = ((System.Collections.Generic.List<string>)(resources.GetObject(\"lmdgv" + cmbTableName.Text + "List.ColumnDesc\")));");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Location = new System.Drawing.Point(0, 284);");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Name = \"lmdgv" + cmbTableName.Text + "List\";");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.ReadOnly = true;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.RowTemplate.Height = 23;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.Size = new System.Drawing.Size(878, 100);");
            code.AppendLine("            this.lmdgv" + cmbTableName.Text + "List.TabIndex = 1;");
            code.AppendLine("            //");
            code.AppendLine("            // frm" + cmbTableName.Text);
            code.AppendLine("            //");
            code.AppendLine("            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);");
            code.AppendLine("            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;");
            code.AppendLine("            this.ClientSize = new System.Drawing.Size(892, 573);");
            code.AppendLine("            this.Controls.Add(this.btnPanel);");
            code.AppendLine("            this.Controls.Add(this.grpCondition);");
            code.AppendLine("            this.Controls.Add(this.grpDetail);");
            code.AppendLine("            this.Controls.Add(this.lmdgv"+cmbTableName.Text+"List);");
            code.AppendLine("            this.Name = \"frm" + cmbTableName.Text + "\";");
            code.AppendLine("            this.Text = \"" + txtFrmTitle.Text + "\";");
            code.AppendLine("            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frm" + cmbTableName.Text + "_FormClosing);");
            code.AppendLine("            this.Load += new System.EventHandler(this.frm" + cmbTableName.Text + "_Load);");
            code.AppendLine("            this.Resize += new System.EventHandler(this.frm" + cmbTableName.Text + "_Resize);");
            code.AppendLine("            this.btnPanel.ResumeLayout(false);");
            code.AppendLine("            this.grpCondition.ResumeLayout(false);");
            code.AppendLine("            this.grpCondition.PerformLayout();");
            code.AppendLine("            this.grpDetail.ResumeLayout(false);");
            code.AppendLine("            this.grpDetail.ResumeLayout(false);");
            code.AppendLine("            ((System.ComponentModel.ISupportInitialize)(this.lmdgv" + cmbTableName.Text + "List)).EndInit();");
            code.AppendLine("            this.ResumeLayout(false);");
            code.AppendLine("");
            code.AppendLine("        }");
            code.AppendLine("");
            code.AppendLine("        #endregion");
            code.AppendLine("");
            code.AppendLine("        private System.Windows.Forms.GroupBox grpDetail;");
            code.AppendLine("        private System.Windows.Forms.GroupBox grpCondition;");
            code.AppendLine("        private System.Windows.Forms.Panel btnPanel;");
            code.AppendLine(EditCtlDeclare.ToString());
            code.AppendLine(QueryCtlDeclare.ToString());
            code.AppendLine("        private LM.ControlLib.LMDataGridView lmdgv" + cmbTableName.Text + "List;");
            code.AppendLine(btnDeclare.ToString());
            code.AppendLine("  }");
            code.AppendLine("}");

            sw.Write(code.ToString());

            sw.Close();
            fs1.Close();

            txtDesignerCS.Clear();
            txtDesignerCS.Text = code.ToString();
        }

        #endregion

        #region 数据库操作类
        
        //生成数据库操作类文件
        private void CreateDBOperClass()
        {
            StringBuilder code = new StringBuilder("");

            MsSql db = new MsSql(cnnStr);
            string sql = @"SELECT col.name AS FieldName, col.name AS FieldText, typ.name + '(' + cast(col.Length as varchar) + ')' AS FieldType,
                            CASE typ.name WHEN 'int' THEN 'int'
                                       WHEN 'varchar' then 'string' 
		                                WHEN 'nvarchar' then 'string' 
		                                WHEN 'smallint' then 'int'
		                                WHEN 'numeric' THEN 'double'
		                                WHEN 'decimal' THEN 'double'
                                        --WHEN 'datetime' THEN 'DateTime'
                                end as vartype
                             FROM syscolumns col
                       INNER JOIN systypes typ ON typ.xtype=col.xtype
                       INNER JOIN sysobjects obj ON obj.ID=col.id
                            WHERE obj.Name='" + cmbTableName.SelectedValue.ToString() + "' ORDER BY col.colid";
            DataSet ds = new DataSet();
            ds = db.ExecuteDataSet(sql);


            FileStream fs1 = new FileStream(filePath + cmbTableName.Text + ".cs", FileMode.Create, FileAccess.Write);//创建写入文件 
            StreamWriter sw = new StreamWriter(fs1);

            code.AppendLine("using System;");
            code.AppendLine("using System.Collections.Generic;");
            code.AppendLine("using System.Linq;");
            code.AppendLine("using System.Text;");
            code.AppendLine("using System.Data;");
            code.AppendLine("");
            code.AppendLine("using LM.DataAccess;");
            code.AppendLine("");
            code.AppendLine("namespace " + txtClassNS.Text);
            code.AppendLine("{");
            code.AppendLine("    public class " + cmbTableName.Text);
            code.AppendLine("    {");
            code.AppendLine("        private string cnnStr;");

            code.AppendLine("");    //数据表字段变量

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                code.AppendLine("        public " + dr["vartype"].ToString() + " " + dr["FieldName"].ToString() + ";");
            }

            code.AppendLine("");

            code.AppendLine("        public " + cmbTableName.Text + "(string dbcnn)");
            code.AppendLine("        {");
            code.AppendLine("            cnnStr = dbcnn;");
            code.AppendLine("        }");
            code.AppendLine("");

            code.AppendLine("        public DataSet Get"+ cmbTableName.Text +"List(string strCondition)");
            code.AppendLine("        {");
            code.AppendLine("            MsSql db = new MsSql(cnnStr);");
            code.AppendLine("            DataSet ds = new DataSet();");
            code.AppendLine("       //下面的sql语句需要根据实际表或视图略作调整");
            code.AppendLine("            string sql = @" + GetDetailSql(ds.Tables[0]) + " WHERE 1=1 \";");
            code.AppendLine("            if(strCondition != \"\")");
            code.AppendLine("                sql = sql + strCondition;");
            code.AppendLine("            ds = db.ExecuteDataSet(sql);");
            code.AppendLine("            return ds;");
            code.AppendLine("        }");

            //此处增加 增删改函数，查？
            //增

            code.AppendLine("//下面相关Sql语句是根据表结构生成的，可能需要需要做调整");
            code.AppendLine("//如果是赋值系统时间的，修改下面sql语句中的相关部分");
            code.AppendLine("//调试完毕后将这个注释删除就可以了");

            code.AppendLine("        public int Add()");
            code.AppendLine("        {");
            code.AppendLine("            int ret = 0;");
            code.AppendLine(@"            //if (!IsExists(OrgCode, OrgName))");
            code.AppendLine(@"                //return -2;");
              
            code.AppendLine("            MsSql db = new MsSql(cnnStr);");

            code.AppendLine("            string sql = @" + GetAddSql(ds.Tables[0]) + ";");
            code.AppendLine("            ret = db.ExecuteNonQuery(sql);");
            code.AppendLine("");
            code.AppendLine("            return ret;");
            code.AppendLine("        }");
            code.AppendLine("");
            //删
            code.AppendLine("        public int Del()");
            code.AppendLine("        {");
            code.AppendLine("            int ret = 0;");

            code.AppendLine("            MsSql db = new MsSql(cnnStr);");

            code.AppendLine("            string sql = @" + GetDelSql(ds.Tables[0]) + ";");
            code.AppendLine("            ret = db.ExecuteNonQuery(sql);");
            code.AppendLine("");
            code.AppendLine("            return ret;");
            code.AppendLine("        }");

            //改
            code.AppendLine("        public int Modify()");
            code.AppendLine("        {");
            code.AppendLine("            int ret = 0;");

            code.AppendLine("            MsSql db = new MsSql(cnnStr);");

            code.AppendLine("            string sql = @" + GetModifySql(ds.Tables[0]) + ";");
            code.AppendLine("            ret = db.ExecuteNonQuery(sql);");
            code.AppendLine("");
            code.AppendLine("            return ret;");
            code.AppendLine("        }");

            //获取详细信息
            code.AppendLine("        public DataSet GetDetail(XXXXXXXXXX)");
            code.AppendLine("        {");
            code.AppendLine("            MsSql db = new MsSql(cnnStr);");
            code.AppendLine("            DataSet ds = new DataSet();");
            code.AppendLine("            string sql = @" + GetDetailSql(ds.Tables[0]) + " WHERE XXXXX= '\";");
            code.AppendLine("            ds = db.ExecuteDataSet(sql);");
            code.AppendLine("            return ds;");
            code.AppendLine("        }");

            code.AppendLine("  }");
            code.AppendLine("}");

            sw.Write(code.ToString());

            sw.Close();
            fs1.Close();

            txtDBClass.Clear();
            txtDBClass.AppendText(code.ToString());
        }

        private string GetAddSql(DataTable dt)
        {
            if(dt == null)
                return "";
            StringBuilder sbFields = new StringBuilder("");
            StringBuilder sbValues = new StringBuilder("");
            StringBuilder ret = new StringBuilder("");

            foreach (DataRow dr in dt.Rows)
            {
                sbFields.Append(dr["FieldName"].ToString() + ",");
                if (dr["vartype"].ToString() == "string")
                {
                    sbValues.Append("'\"+" + dr["FieldName"].ToString() + "+\"',");
                }
                else if (dr["vartype"].ToString() == "int" || dr["vartype"].ToString() == "double")
                {
                    sbValues.Append("\"+" + dr["FieldName"].ToString() + ".ToString() + \",");
                }
            }

            ret.Append("\"INSERT INTO " + cmbTableName.SelectedValue.ToString() + "(");
            ret.Append(sbFields.ToString().Substring(0, sbFields.ToString().Length - 1));
            ret.Append(") values(");
            ret.Append(sbValues.ToString().Substring(0, sbValues.ToString().Length - 1));
            ret.Append(")\"");
            return ret.ToString();
        }

        private string GetDelSql(DataTable dt)
        {
            if (dt == null)
                return "";
            StringBuilder ret = new StringBuilder("");

            //ret.Append("\"DELETE FROM " + cmbTableName.SelectedValue.ToString() + " WHERE ID ='\"");

            ret.Append("\"UPDATE " + cmbTableName.SelectedValue.ToString() + " SET IsUse=0 WHERE ID ='\"");

            return ret.ToString();
        }

        private string GetModifySql(DataTable dt)
        {
            if (dt == null)
                return "";
            string sbFields;
            string sbValues;
            StringBuilder ret = new StringBuilder("");
            StringBuilder sql = new StringBuilder("");
            sbFields = "";
            sbValues = "";

            foreach (DataRow dr in dt.Rows)
            {
                sbFields = dr["FieldName"].ToString();
                
                if (dr["vartype"].ToString() == "string")
                {
                    sbValues = "'\"+" + dr["FieldName"].ToString() + "+\"'";
                }
                else if (dr["vartype"].ToString() == "int" || dr["vartype"].ToString() == "double")
                {
                    sbValues = "\"+" + dr["FieldName"].ToString() + ".ToString() + \"";
                }
                if (sql.Length == 0)
                    sql.Append(sbFields + " = " + sbValues);
                else
                    sql.Append(", " + sbFields + " = " + sbValues);
            }
            ret.Append("\"UPDATE " + cmbTableName.SelectedValue.ToString() + " SET ");
            ret.Append(sql.ToString());
            ret.Append(" WHERE XXXXXXXX= \"");
            return ret.ToString();
        }

        private string GetDetailSql(DataTable dt)
        {
            if (dt == null)
                return "";
            StringBuilder sbFields = new StringBuilder("");
            StringBuilder ret = new StringBuilder("");

            foreach (DataRow dr in dt.Rows)
            {
                sbFields.Append(dr["FieldName"].ToString() + ",");
            }
            ret.Append("\"SELECT "); 
            ret.Append(sbFields.ToString().Substring(0, sbFields.ToString().Length - 1));
            ret.Append(" FROM " + cmbTableName.SelectedValue.ToString());
            return ret.ToString();
        }

        #endregion

        private void Form1_Resize(object sender, EventArgs e)
        {
            dataGridView1.Height = tabPage8.Height - 127;
            if (this.Width < 834)
                this.Width = 834;
            if (this.Height < 640)
                this.Height = 640;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }
        
    }
}
