using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Genetics;
using Matricies;
using NeuralNetworks;


namespace Genetic_Brachistochrone
{
    public partial class Form1 : Form
    {
        Curve[] curves = new Curve[98];
        Bitmap scrn = new Bitmap(1344, 800);
        Graphics graphicsObj;

        int generationNum = 1;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            graphicsObj = Graphics.FromImage(scrn);

            for (int i = 0; i < curves.Length; i++)
            {
                //Optimize this later...
                int numHid = Constants.numHidden;
                int numOut = Constants.numOutput;
                Genome genome = new Genome((numOut + numHid * (4 + numOut)) * 16); //(#w + #b + 1 ) * 16
                curves[i] = new Curve(Constants.numHidden, Constants.numOutput, genome);
            }
        }

        private static Curve[] reproduce(Curve[] parents, double dblRate, double mutRate)
        {
            Curve[] children = new Curve[parents.Length];
            Random rnd = new Random();

            //Sort parents by numEaten
            Array.Sort(parents,
                delegate (Curve par1, Curve par2) { return par1.time.CompareTo(par2.time); });

            //Keep 20% of the best curves
            for (int i = 0; i < (int)(children.Length * .2); i++)
            {
                children[i] = parents[i];
            }

            //For the other 80%, choose two of the best 20% and make a child
            for (int i = (int)(children.Length * .2); i < children.Length; i++)
            {
                //Randomly choose parents from the top 20%
                int randFatherIndex = rnd.Next(0, (int)(children.Length * .2));
                int randMotherIndex = rnd.Next(0, (int)(children.Length * .2));

                //Create a genome for the child from the genomes of the parents
                Genome childGenome = parents[randFatherIndex]
                    .genome.offspring(parents[randMotherIndex].genome, dblRate, mutRate);

                children[i] = new Curve(Constants.numHidden, Constants.numOutput, childGenome);
            }

            //Reset all positions and numEatens

            return children;
        }

        private void tmrCurves_Tick(object sender, EventArgs e)
        {
            generationNum++; lblGeneration.Text = "Generation: " + generationNum;

            graphicsObj.Clear(Color.White);
            
            for (int i = 0; i < curves.Length; i++)
            {
                curves[i].time = curves[i].getTime();

                //Draw
                if (i < 100)
                {
                    int x = (96 * i) % 1344;
                    graphicsObj.DrawImage(curves[i].draw(8), x, 40 + ((96 * i) - x) / 1344 * 96);
                }
            }

            curves = reproduce(curves, .5, .01);

            lblTime.Text = "Fastest Time: " + Math.Round(curves[0].time, 10) + " Sec";

            Invalidate();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.DrawImage(scrn, 0, 0);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            tmrCurves.Enabled = true;
        }
    }

    public class Curve
    {
        public Network curveGenerator { get; set; }
        public Genome genome { get; set; }
        public double time { get; set; }

        public Curve(int numHid, int numOut, Genome inGenome)
        {
            genome = inGenome;

            curveGenerator = new Network(2, numHid, numOut, inGenome);
        }

        public double getTime()
        {
            double[,] input = new double[2, 1];
            input[0, 0] = Constants.height;
            input[1, 0] = Constants.dist;

            curveGenerator.feedforward(input);

            double[,] heights = new double[curveGenerator.output.GetLength(0) + 2, 1];

            for (int i = 0; i < curveGenerator.output.GetLength(0); i++)
            {
                heights[i + 1, 0] = curveGenerator.output[i, 0];
            }

            heights[0, 0] = Constants.height;
            heights[curveGenerator.output.GetLength(0) + 1, 0] = .2;

            double vel = 0;
            double time = 0;

            for (int i = 1; i < heights.GetLength(0); i++)
            {
                double deltaY = (heights[i, 0] - heights[i - 1, 0]);
                double deltaX = Constants.dist / (curveGenerator.output.GetLength(0) + 1);
                double theta = -Math.Atan(deltaY / deltaX);

                if (vel * vel + 2 * 9.8 * Math.Sqrt(deltaX * deltaX + deltaY * deltaY) * Math.Sin(theta) < 0)
                {
                    return i * deltaX + 100;
                }

                double tempTime = (-vel + Math.Sqrt(vel * vel + 2 * 9.8 * Math.Sqrt(deltaX * deltaX + deltaY * deltaY) * Math.Sin(theta))) / (9.8 * Math.Sin(theta));
                vel += tempTime * 9.8 * Math.Sin(theta);
                time += tempTime;
            }

            return time;
        }

        public Bitmap draw(int scale)
        {
            Bitmap curvePic = new Bitmap(scale * (Constants.numOutput + 1), scale * (Constants.numOutput + 1));
            Graphics graphicsObj;
            graphicsObj = Graphics.FromImage(curvePic);
            Pen penObj = new Pen(Color.Black, 1);

            graphicsObj.DrawLine(penObj, 0, 0, scale, (float)(curvePic.Height * (1 - curveGenerator.output[0, 0])));

            for (int i = 0; i < curveGenerator.output.GetLength(0) - 1; i++)
            {
                graphicsObj.DrawLine(penObj, scale * (i + 1), (float)(curvePic.Height * (1 - curveGenerator.output[i, 0])),
                    scale * (i + 2), (float)(curvePic.Height * (1 - curveGenerator.output[i + 1, 0])));
            }

            graphicsObj.DrawLine(penObj, curvePic.Width - scale,
                (float)(curvePic.Height * (1 - curveGenerator.output[Constants.numOutput - 1, 0])),
                curvePic.Width, (float)(curvePic.Height * .8));

            return curvePic;
        }
    }

    class Constants
    {
        public const int numHidden = 10;
        public const int numOutput = 10;
        public const double height = 1;
        public const double dist = 1;
    }
}
