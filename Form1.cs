using Esprima;
using Esprima.Ast;
using Esprima.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JSFunctionAnalyzer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog selectFile = new OpenFileDialog();
            if (selectFile.ShowDialog() == DialogResult.OK)
            {
                textBox1.Text = selectFile.InitialDirectory + selectFile.FileName;
            }
            selectFile.Dispose();
        }

        private void Parse(object sender, EventArgs e)
        {
            // Parsing JS file into object structure
            Expression script = null;
            try
            {
                string sr = File.ReadAllText(Path.GetFullPath(textBox1.Text));
                JavaScriptParser parser = new JavaScriptParser(sr);
                script = parser.ParseExpression();
            }
            catch (Exception)
            {
                MessageBox.Show("Path is invalid: " + textBox1?.Text ?? "Textbox is null", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Getting body node of JS structure

            Node objectNode = GetFirstNodeOfType(script, Nodes.ObjectExpression);

            List<string> returnFunctions = new List<string>();
            Dictionary<string, int> paramsFunctions = new Dictionary<string, int>();

            // Going through all child nodes looking for required

            foreach (Node node in objectNode.ChildNodes)
            {
                if (node.Type != Nodes.Property)
                {
                    continue;
                }
                Node functionExpressionNode = GetFirstNodeOfType(node, Nodes.FunctionExpression);
                if (functionExpressionNode != null)
                {
                    // Retrieving name

                    Node identifierNode = GetFirstNodeOfType(node, Nodes.Identifier);
                    if (identifierNode == null)
                    {
                        MessageBox.Show("One of functions has null name, input file is invalid or has errors.");
                        continue;
                    }
                    var temp = JsonConvert.DeserializeObject<Dictionary<string, string>>(identifierNode.ToJsonString());
                    temp.TryGetValue("name", out string name);

                    // Return functions

                    Node blockStatementNode = GetFirstNodeOfType(node, Nodes.BlockStatement);
                    if (blockStatementNode != null)
                    {
                        if (blockStatementNode.ChildNodes.Any(x => x.Type == Nodes.ReturnStatement))
                        {
                            returnFunctions.Add(name);
                        }
                    }

                    // Params functions

                    int countOfParams = CountParamsOfFunction(functionExpressionNode);
                    if (countOfParams > 0)
                    {
                        paramsFunctions.Add(name, countOfParams);
                    }

                }
            }

            // Outputting text

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Functions with return statement:");
            sb.AppendLine(string.Join(",", returnFunctions));
            sb.AppendLine();
            sb.AppendLine("Functions with parameters:");
            foreach (var dict in paramsFunctions)
            {
                sb.AppendLine(dict.Key + ": " + dict.Value);
            }

            textBox2.Text = sb.ToString();
        }

        // Helper function, looks through all child nodes and their child nodes in recursion
        private Node GetFirstNodeOfType(Node node, Nodes type)
        {
            if (node == null || node.Type == type) return node;
            foreach (var childNode in node.ChildNodes)
            {
                Node foundNode = GetFirstNodeOfType(childNode, type);
                if (foundNode != null) return foundNode;
            }
            return null;
        }

        // Helper function, counts amount of parameters in JS function
        private int CountParamsOfFunction(Node node)
        {
            if (node.Type != Nodes.FunctionExpression) return -1;
            int count = 0;
            foreach (Node child in node.ChildNodes)
            {
                if (child == null) continue;
                if (child.Type == Nodes.Identifier) count++;
            }
            return count;
        }
    }
}
