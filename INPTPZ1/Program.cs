using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.Drawing.Text;

namespace cmplx
{
    class Program
    {
        static void Main(string[] args)
        {
            double xMin = -1.5;
            double xMax = 1.5;
            double yMin = -1.5;
            double yMax = 1.5;
            int scale = 100;

           string filename = "out.png";

            if (args.Length == 0)
            {
                filename = "out.png";
                xMin = -1.5;
                xMax = 1.5;
                yMin = -1.5;
                yMax = 1.5;
                scale = 100;              

            }
            else if (args.Length == 1)
            {
                filename = args[0];
            }
            else if(args.Length == 6)
            {
                filename = args[0];
                xMin = int.Parse(args[1]);
                xMax = int.Parse(args[2]);
                yMin = int.Parse(args[3]);
                yMax = int.Parse(args[4]);
                scale = int.Parse(args[5]);
            }            
            Console.WriteLine("Calculation of fractal");            
            Complex[] coefficients = { Complex.One, Complex.Zero, Complex.Zero, Complex.One };
            Polynomial polynom = new Polynomial(coefficients);
            NewtonFractal fractal = new NewtonFractal(polynom, xMin, xMax, yMin, yMax, scale);            
            Bitmap image = fractal.DrawFractal();
            image.Save(filename);
            Console.WriteLine("The fractal was successfully built.");
            Console.ReadKey();
        }
    }

    class Complex
    {
        public double Real { get; set; }
        public double Imaginary { get; set; }

        public readonly static Complex Zero = new Complex(0, 0);
        public readonly static Complex One = new Complex(1, 0);
        public readonly static Complex ImaginaryOne = new Complex(0, 1);

        public Complex(double re, double im)
        {
            Real = re;
            Imaginary = im;
        }

        public static Complex Multiply(Complex a, Complex b)
        {
            // aRe*bRe + aRe*bIm*i + aIm*bRe*i + aIm*bIm*i*i                      
            double re = a.Real * b.Real - a.Imaginary * b.Imaginary;
            double im = a.Real * b.Imaginary + a.Imaginary * b.Real;
            return new Complex(re, im);
        }


        public static Complex Add(Complex a, Complex b)
        {
            double re = a.Real + b.Real;
            double im = a.Imaginary + b.Imaginary;
            return new Complex(re, im);
        }

        public static Complex operator +(Complex a, Complex b)
        {
            return Add(a, b);
        }
        public static Complex operator -(Complex a, Complex b)
        {
            return Subtract(a, b);
        }
        public static Complex operator *(Complex a, Complex b)
        {
            return Multiply(a, b);
        }
        public static Complex operator /(Complex a, Complex b)
        {
            return Divide(a, b);
        }

        public static Complex Subtract(Complex a, Complex b)
        {
            double re = a.Real - b.Real;
            double im = a.Imaginary - b.Imaginary;
            return new Complex(re, im);
        }

        public override string ToString()
        {
            return $"({Real} + {Imaginary}i)";
        }

        public static Complex Divide(Complex a, Complex b)
        {
            // (aRe + aIm*i) / (bRe + bIm*i)
            // ((aRe + aIm*i) * (bRe - bIm*i)) / ((bRe + bIm*i) * (bRe - bIm*i))
            //  bRe*bRe - bIm*bIm*i*i
            Complex tmp = Multiply(a, new Complex(b.Real, -b.Imaginary));
            double tmp2 = b.Real * b.Real + b.Imaginary * b.Imaginary;
            return new Complex(tmp.Real / tmp2, tmp.Imaginary / tmp2);
        }

        public static double Abs(Complex a)
        {
            return Math.Sqrt(a.Real * a.Real + a.Imaginary * a.Imaginary);
        }
        public static Complex Pow(Complex a, int power)
        {
            Complex bx = a;
            if (power > 0)
            {
                for (int i = 0; i < power - 1; i++)
                {
                    bx = bx * a;
                }
                return bx;
            }
            return Complex.One;
        }
    }

    class Polynomial
    {
        public Complex[] coefficients { get; set; }
        public int Degree { get; private set; }
        public Polynomial(int degree)
        {
            coefficients = new Complex[degree + 1];
        }
        public Polynomial(Complex[] coeff)
        {
            coefficients = coeff;

            this.Degree = coeff.Length - 1;
        }

        public Polynomial Derive()
        {
            Polynomial derivative = new Polynomial(Degree - 1);
            for (int i = 1; i < coefficients.Length; i++)
            {
                derivative.coefficients[i - 1] = this.coefficients[i] * new Complex(i, 0);
            }

            return derivative;
        }

        public Complex Eval(Complex x)
        {
            Complex sum = Complex.Zero;
            for (int i = 0; i < coefficients.Length; i++)
            {
                sum += coefficients[i] * Complex.Pow(x, i);
            }

            return sum;
        }

        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < coefficients.Length; i++)
            {
                s += coefficients[i];
                if (i > 0)
                {
                    for (int j = 0; j < i; j++)
                    {
                        s += "x";
                    }
                }
                s += " + ";
            }
            return s.Substring(0, s.Length - 3);
        }
    }
    class NewtonFractal
    {       
        private Bitmap bmp;
        private int maxId = 0;
        private Polynomial polynom;
        private List<Complex> roots;
        private Polynomial derivative;
        private double xstep;
        private double ystep;
        private double xMin;
        private double yMin;
        private int width;
        private int height;
        private Color[] colors;
        private Complex ox;
        private int id;
        private double eps1 = 0.0001;

        public NewtonFractal(Polynomial polynom, double xMin, double xMax, double yMin, double yMax, int scale)
        {
            this.polynom = polynom;
            width = (int)Math.Round((xMax - xMin) * scale);
            height = (int)Math.Round((yMax - yMin) * scale);
            bmp = new Bitmap(width, height);
            xstep = (xMax - xMin) / width;
            ystep = (yMax - yMin) / height;
            this.xMin = xMin;
            this.yMin = yMin;

            roots = new List<Complex>();

            derivative = polynom.Derive();
            colors = new Color[] { Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Orange, Color.Fuchsia, Color.Gold, Color.Cyan, Color.Magenta };
        }

        private int FindSolution()
        {
            int iteration = 0;
            for (int q = 0; q < 30; q++)
            {
                Complex diff = polynom.Eval(ox) / derivative.Eval(ox);
                ox = ox - diff;

                //Console.WriteLine($"{q} {ox} -({diff})");
                if (Complex.Abs(diff) >= Math.Sqrt(0.5))
                {
                    q--;
                }
                iteration++;
            }
            return iteration;
        }
        private int FindRootNumber()
        {
            // find solution root number
            bool known = false;
            int rootNumber = 0;
            for (int w = 0; w < roots.Count; w++)
            {
                if (Complex.Abs(ox - roots[w]) <= 0.1)
                {
                    known = true;
                    rootNumber = w;
                }
            }
            if (!known)
            {
                roots.Add(ox);
                rootNumber = roots.Count;
                maxId = rootNumber + 1;
            }
            return rootNumber;
        }

        private Complex GetInitialValue(int j, int i)
        {
            // find "world" coordinates of pixel
            double x = xMin + j * xstep;
            double y = yMin + i * ystep;

            Complex initial = new Complex(x, y);

            if (initial.Real == 0)
                initial += new Complex(eps1, 0);
            if (initial.Imaginary == 0)
                initial += new Complex(0, eps1);
            return initial;
        }
        public Bitmap DrawFractal()
        {
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    ox = GetInitialValue(j, i);
                    int iteration = FindSolution();

                    id = FindRootNumber();

                    Color vv = colors[id % colors.Length];
                    bmp.SetPixel(j, i, vv);
                }
            }
            return bmp;
        }
    }
}

