using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

using System.Xml.Linq;

namespace VisibilityGraph
{
    public partial class Form1 : Form
    {

        List<List<PointFP>> Polygons;
        List<PointFP> GlobalVertexes;
        List<edge> GlobalEdges;

        List<int> Order;

        float[,] VisabilityMatrix;

        Graphics graph;

        string NameOfFile = "output.txt";

        System.Drawing.SolidBrush FigureBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Yellow);
        System.Drawing.SolidBrush NodeBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
        System.Drawing.SolidBrush RedBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Red);
        System.Drawing.SolidBrush GreenBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Green);
        Pen LineBrush = new Pen(Brushes.Red);

        class edge
        {
            public PointF a = new PointF(0,0);
            public PointF b = new PointF(0,0);

            public edge(PointF A, PointF B)
            {
                a = A;
                b = B;

            }

            public bool isEdgeVertex(PointF t)
            {
                bool res = a == t || b == t;
                return res;
            }

        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Text files(*.xml)|*.xml|All files(*.*)|*.*";

            Polygons = new List<List<PointFP>>();
            GlobalVertexes = new List<PointFP>();
            GlobalEdges = new List<edge>();

            Order = new List<int>();

            graph = CreateGraphics();
        }

        private int bypass(int max, List<PointFP> pol)
        {
            if (pol.Count() < 3)
                return 1;

            float res = SideVectorPoint(pol[max].ToPointF(),pol[(max - 1 + pol.Count())% pol.Count()].ToPointF(),pol[(max + 1 ) % pol.Count()].ToPointF());
            if (res > 0)
                return 1;
            return -1;
        }

        private void LoadFile_Click(object sender, EventArgs e)
        {
            visibility.Enabled = false;
            button1.Enabled = false;
            checkBox1.Enabled = false;

            if (openFileDialog1.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            string filename = openFileDialog1.FileName;
            XDocument xdoc = XDocument.Load(filename);

            Polygons.Clear();
            GlobalVertexes.Clear();
            GlobalEdges.Clear();
            Order.Clear();

            int polygonCounter = 0;
            int globalPointCounter = 0;
            foreach (XElement polygonElement in xdoc.Element("polygons").Elements("polygon"))
            {
                List<PointFP> polygonVertexes = new List<PointFP>();
                int pointCounter = 0;
                int MaxX = 0;
                foreach (XElement pointElement in polygonElement.Elements("point"))
                {
                    XAttribute pointX = pointElement.Attribute("x");
                    XAttribute pointY = pointElement.Attribute("y");

                    if (pointX != null && pointY != null)
                    {
                        PointFP Vertex = new PointFP((float)pointX, (float)pointY, polygonCounter, pointCounter, globalPointCounter);
                        polygonVertexes.Add(Vertex);
                        GlobalVertexes.Add(Vertex);

                        if (Vertex.X > polygonVertexes[MaxX].X)
                            MaxX = pointCounter;

                        pointCounter++;
                        globalPointCounter++;
                    }
                }
                Order.Add(bypass(MaxX, polygonVertexes));
                Polygons.Add(polygonVertexes);
                polygonCounter++;
                visibility.Enabled = true;

            }

            foreach (List<PointFP> polygon in Polygons)
            {
                for (int i = 0; i < polygon.Count(); i++)
                {
                    GlobalEdges.Add(new edge(polygon[i].ToPointF(), polygon[(i+1)% polygon.Count()].ToPointF()));
                }
            }

            VisabilityMatrix = new float[GlobalVertexes.Count(), GlobalVertexes.Count()];

            DrawScene();
        }

        private PointF[] ToArray(List<PointFP> l)
        {
            List<PointF> t = new List<PointF>();

            foreach( PointFP p in l)
            {
                t.Add(p.ToPointF());
            }
            return t.ToArray();
        }

        private void DrawScene()
        {
            graph.Clear(Color.White);
            foreach (List<PointFP> polygon in Polygons)
            {
                if (polygon.Count > 2) // Make a polygons
                    graph.FillPolygon(FigureBrush, ToArray(polygon));
                foreach (PointFP node in polygon) //Draw all points
                    graph.FillRectangle(NodeBrush, node.X - 2, node.Y - 2, 4, 4);
            }
            textBox1.Text = "Scene ready";
        }

        private void DrawWays()
        {
           

            int lenght = GlobalVertexes.Count();
            for (int i = 0; i < lenght; i++)
            {
                PointFP A = Polygons[GlobalVertexes[i].polygonIndex][GlobalVertexes[i].pointIndex];
                if (isConvexVertex(A, Polygons))
                    graph.FillRectangle(GreenBrush, GlobalVertexes[i].X - 2, GlobalVertexes[i].Y - 2, 4, 4);
                else
                    graph.FillRectangle(RedBrush, GlobalVertexes[i].X - 2, GlobalVertexes[i].Y - 2, 4, 4);

                //if (VisabilityMatrix[i, i] != float.MaxValue)
                //    graph.FillRectangle(GreenBrush, GlobalVertexes[i].X - 2, GlobalVertexes[i].Y - 2, 4, 4);
                //else
                //    graph.FillRectangle(RedBrush, GlobalVertexes[i].X - 2, GlobalVertexes[i].Y - 2, 4, 4);
            
                for (int j = i+1; j < lenght; j++)
                {
                    if (VisabilityMatrix[i, j] != float.MaxValue)
                    {
                        graph.DrawLine(new Pen(GreenBrush), GlobalVertexes[i].ToPointF(), GlobalVertexes[j].ToPointF());
                    }
                    else if (checkBox1.Checked)
                    {
                        graph.DrawLine(new Pen(RedBrush), GlobalVertexes[i].ToPointF(), GlobalVertexes[j].ToPointF());
                    }
                }
            }
        }

        class PointFP
        {
            public float X = 0;
            public float Y = 0;
            public float angle = 0;
            public int polygonIndex = 0;
            public int pointIndex = 0;
            public int globalIndex = 0;

            public PointFP()
            {
            }

            public PointFP(float x, float y, int poly, int index,int glob)
            {
                X = x;
                Y = y;
                polygonIndex = poly;
                pointIndex = index;
                globalIndex = glob;
                angle = (float)Math.Atan2(Y, X);
            }

            public PointF ToPointF()
            {
                return new PointF(X, Y);
            }
        
        }

        private bool isConvexVertex(PointFP t, List<List<PointFP>> polygons)
        {
            List<PointFP> polygon = polygons[t.polygonIndex];
            return SideVectorPoint(polygon[(t.pointIndex + polygon.Count() - 1) % polygon.Count()].ToPointF(),
                                   polygon[(t.pointIndex + 1) % polygon.Count()].ToPointF(), t.ToPointF())* Order[t.polygonIndex] >= 0;
        }

        private float SideVectorPoint(PointF A, PointF B, PointF P)
        {
            PointF VecU = new PointF(B.X - A.X, B.Y - A.Y);
            PointF VecV = new PointF(P.X - A.X, P.Y - A.Y);

            return (VecU.X * VecV.Y - VecU.Y * VecV.X);
        }

        private bool isIntersection(PointF StartF, PointF EndF, PointF StartS, PointF EndS)
        {
            if (EndF == StartS || EndF == EndS)
                return false;

            if (StartF == StartS || StartF == EndS)
                return false;

            double det = (EndF.X - StartF.X) * (StartS.Y - EndS.Y) -
                (EndF.Y - StartF.Y) * (StartS.X - EndS.X);

            if (det == 0.0)
            {
                return false;
            }

            double det1 = (StartS.X - StartF.X) * (StartS.Y - EndS.Y) -
                (StartS.Y - StartF.Y) * (StartS.X - EndS.X);

            double det2 = (EndF.X - StartF.X) * (StartS.Y - StartF.Y) -
                (EndF.Y - StartF.Y) * (StartS.X - StartF.X);

            double t = det1 / det;
            double r = det2 / det;

            if ((0 <= t) && (t <= 1) && (0 <= r) && (r <= 1))
            {
                return true;
            }
            return false;
        }

        private bool isSectorVertex(PointFP v, PointFP t)
        {
            List<PointFP> polygon = Polygons[v.polygonIndex];

            PointFP left = polygon[(polygon.Count() + v.pointIndex - 1) % polygon.Count()];
            PointFP right = polygon[(v.pointIndex + 1) % polygon.Count()];

            float lr = SideVectorPoint(v.ToPointF(), left.ToPointF(), t.ToPointF());
            float rr = SideVectorPoint(v.ToPointF(), right.ToPointF(), t.ToPointF());

            lr *= Order[v.polygonIndex];
            rr *= Order[v.polygonIndex];

            if (lr > 0 && rr < 0) return true;

            return false;
        }

        private string StringOfArr(int indexOfString)
        {
            string answer = "";
            for (int i = 0; i < GlobalVertexes.Count(); i++)
            {
                answer += VisabilityMatrix[indexOfString, i].ToString();
                answer += "\t";
            }
            return answer;
        }

        private void WriteFile()
        {
            StreamWriter writer = File.CreateText(NameOfFile);
            for (int i = 0; i < GlobalVertexes.Count(); i++)
            {
                writer.WriteLine(StringOfArr(i));
            }
            writer.Close();
            textBox1.Text = "File ready";
        }

        private void visibility_Click(object sender, EventArgs e)
        {
            if (GlobalVertexes.Count() == 0)
                return;
            
            for(int i=0;i< GlobalVertexes.Count(); i++)
            {
                PointFP v = GlobalVertexes[i];

                if (isConvexVertex(v, Polygons))
                {
                    for(int k=0;k< GlobalVertexes.Count();k++)
                    {
                        if (i == k)
                            continue;
                        PointFP point = GlobalVertexes[k];

                        bool visibility = true;

                        // проверить что point не лежит в конусе образованном ребрами V
                        if (isSectorVertex(v, point))
                        {
                            visibility = false;
                        }
                        
                        for(int j = 0; j < GlobalEdges.Count() && visibility; j++)
                        {

                            if (isIntersection(v.ToPointF(), point.ToPointF(), GlobalEdges[j].a, GlobalEdges[j].b))
                            {
                                visibility = false;
                            }
                                
                        }

                        if (visibility)
                        {
                            float dist = (float)Math.Sqrt((double)(point.X - v.X) * (point.X - v.X) + (double)(point.Y - v.Y) * (point.Y - v.Y));
                            VisabilityMatrix[i, point.globalIndex] = dist;
                            // записать в матрицу в клетку [i][k] расстояние между v и point
                            // и в [k][i]
                        }
                        else
                        {
                            VisabilityMatrix[i, point.globalIndex] = float.MaxValue;
                            // максимальное число в записать в ячейки
                        }
                        VisabilityMatrix[i, i] = 0;
                    }

                }
                else
                {
                    for(int t=0;t< GlobalVertexes.Count();t++)
                    {
                        VisabilityMatrix[i, t] = float.MaxValue;
                        VisabilityMatrix[t, i] = float.MaxValue;
                        if (t == i)
                            VisabilityMatrix[i, t] = 0;
                    }
                    // записываем максимальные значения в матрицу ( строка и столбец)
                }
            }
            DrawWays();
            button1.Enabled = true;
            checkBox1.Enabled = true;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            WriteFile();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            graph.Clear(Color.White);
            DrawScene();
            DrawWays();
        }
    }
}
