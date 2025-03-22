using System;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace UI_Demo;

public class PlotFunctionHelper
{
    /// <summary>
    /// Standard Linear Function, e.g. y=2x+3
    /// </summary>
    /// <param name="input">the value to operate on</param>
    /// <param name="slope">the slope</param>
    /// <param name="intercept">the y-intercept</param>
    public static double LinearFunction(double input, double slope, double intercept) => slope * input + intercept;

    /// <summary>
    /// Normal Distribution Curve Function.
    /// </summary>
    /// <param name="mean">the center of the bell curve</param>
    /// <param name="standardDeviation">controls the width of the curve</param>
    /// <param name="numPoints">the number of points to generate for the curve</param>
    /// <param name="range">the range of x-values to cover (centered around the mean)</param>
    public static double[] BellCurveFunction(double mean, double standardDeviation, int numPoints, double range)
    {
        double minX = mean - range;
        double maxX = mean + range;
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate x-value for each point
            double x = minX + (maxX - minX) * i / (numPoints - 1);
            // Calculate y-value using the normal distribution formula
            yValues[i] = (1 / (standardDeviation * Math.Sqrt(Extensions.Tau))) * Math.Exp(-Math.Pow(x - mean, 2) / (2 * Math.Pow(standardDeviation, 2)));
        }
        return yValues;
    }

    /// <summary>
    /// Normal Distribution Curve Function.
    /// </summary>
    /// <param name="mean">the center of the bell curve</param>
    /// <param name="standardDeviation">controls the width of the curve</param>
    /// <param name="numPoints">the number of points to generate for the curve</param>
    /// <param name="range">the range of x-values to cover (centered around the mean)</param>
    /// <param name="multiplier">the amount to multiply the final value by</param>
    public static double[] BellCurveFunction(double mean, double standardDeviation, int numPoints, double range, double multiplier)
    {
        double minX = mean - range;
        double maxX = mean + range;
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate x-value for each point
            double x = minX + (maxX - minX) * i / (numPoints - 1);
            // Calculate y-value using the normal distribution formula
            yValues[i] = ((1 / (standardDeviation * Math.Sqrt(Extensions.Tau))) * Math.Exp(-Math.Pow(x - mean, 2) / (2 * Math.Pow(standardDeviation, 2)))) * (multiplier > 0 ? multiplier : 1);
        }
        return yValues;
    }

    /// <summary>
    /// Method to generate points for a saw-tooth wave.
    /// </summary>
    /// <param name="frequency">the frequency of the sawtooth wave (in Hz)</param>
    /// <param name="amplitude">the height of the wave</param>
    /// <param name="duration">the total time duration for which the wave is generated</param>
    /// <param name="numPoints">the number of points to generate for the wave</param>
    /// <returns></returns>
    public static double[] SawtoothWaveFunction(double frequency, double amplitude, int numPoints, double duration, double verticalShift = 0)
    {
        double period = 1.0 / frequency; // period of the wave
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate x-value for each point
            double x = duration * i / (numPoints - 1);
            // Calculate y-value using the sawtooth wave formula
            yValues[i] = amplitude * (x % period) / period + verticalShift;
        }
        return yValues;
    }

    /// <summary>
    /// Method to generate points for a square wave.
    /// </summary>
    /// <param name="frequency">the frequency of the square wave (in Hz)</param>
    /// <param name="amplitude">the height of the wave</param>
    /// <param name="numPoints">the number of points to generate for the wave</param>
    /// <param name="duration">the total time duration for which the wave is generated</param>
    /// <returns></returns>
    public static double[] SquareWaveFunction(double frequency, double amplitude, int numPoints, double duration, double verticalShift = 0)
    {
        double period = 1.0 / frequency; // period of the wave
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate x-value for each point
            double x = duration * i / (numPoints - 1);
            // Calculate y-value using the square wave formula
            yValues[i] = ((x % period) < (period / 2) ? amplitude : -amplitude) + verticalShift;
        }
        return yValues;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="frequency">the frequency of the square wave (in Hz)</param>
    /// <param name="amplitude">the height of the wave</param>
    /// <param name="numPoints">the number of points to generate for the wave</param>
    /// <param name="duration">the total time duration for which the wave is generated</param>
    /// <param name="roundnessFactor">controls the smoothness of the rounded peaks (higher value = sharper peaks)</param>
    /// <returns></returns>
    public static double[] SquareWaveRoundedFunction(double frequency, double amplitude, int numPoints, double duration, double roundnessFactor, double verticalShift)
    {
        double period = 1.0 / frequency; // period of the wave
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate x-value for each point
            double x = duration * i / (numPoints - 1);
            // Calculate y-value using the rounded square wave formula
            double normalizedX = (x % period) / period; // Normalize x to 0-1
            if (normalizedX < 0.25)
                yValues[i] = amplitude * Math.Sin(Math.PI * normalizedX * 4) + verticalShift;
            else if (normalizedX < 0.75)
                yValues[i] = amplitude + verticalShift;
            else
                yValues[i] = amplitude * Math.Sin(Math.PI * (normalizedX - 1) * 4) + verticalShift;
        }
        return yValues;
    }

    /// <summary>
    /// Sinusoidal function generator.
    /// </summary>
    public static double[] SineWaveFunction(double frequency, double amplitude, int numPoints, double phaseShift = 0)
    {
        List<double> yValues = new();
        for (double t = 1; t <= numPoints; t += 0.1)
        {
            // Calculate y-value using the sine wave formula
            yValues.Add(amplitude * Math.Sin(Extensions.Tau * frequency * t + phaseShift));
        }
        return yValues.ToArray();
    }

    /// <summary>
    /// Cosine function generator.
    /// </summary>
    public static double[] CosineWaveFunction(double frequency, double amplitude, int numPoints, double phaseShift = 0)
    {
        List<double> yValues = new();
        for (double t = 1; t <= numPoints; t += 0.1)
        {
            // Calculate y-value using the sine wave formula
            yValues.Add(amplitude * Math.Cos(Extensions.Tau * frequency * t + phaseShift));
        }
        return yValues.ToArray();
    }

    /// <summary>
    /// Tangent function generator.
    /// </summary>
    public static double[] TangentWaveFunction(double frequency, double amplitude, int numPoints, double phaseShift = 0)
    {
        List<double> yValues = new();
        for (double t = 1; t <= numPoints; t += 0.1)
        {
            // Calculate y-value using the sine wave formula
            yValues.Add(amplitude * Math.Tan(Extensions.Tau * frequency * t + phaseShift));
        }
        return yValues.ToArray();
    }

    /// <summary>
    /// Experimental color gradient mapping function. 
    /// </summary>
    /// <remarks>
    /// Basic formula is "sin(x) + cos(y)".
    /// </remarks>
    public static double[] GradientMappingFunction(double frequency, double amplitude, int numPoints, double phaseShift = 0)
    {
        List<double> yValues = new();
        for (double t = 1; t <= numPoints; t += 0.1)
        {
            // Calculate x-value for each point
            double x = t * 1 / (numPoints - 1);

            // Calculate y-value using the sine wave formula
            yValues.Add(amplitude * Math.Sin(Extensions.Tau * frequency * x + phaseShift) + Math.Cos(Extensions.Tau * frequency * t + phaseShift));
        }
        return yValues.ToArray();
    }

    /// <summary>
    /// Models growth that saturates at a maximum value.
    /// </summary>
    /// <param name="capacity">The maximum value the function approaches (carrying capacity)</param>
    /// <param name="rate">The growth rate</param>
    /// <param name="center">The x-value where the sigmoid curve is centered</param>
    /// <param name="minX">The minimum x-value for the plot</param>
    /// <param name="maxX">The maximum x-value for the plot</param>
    /// <param name="numPoints">The number of points to generate for the plot</param>
    public static double[] LogisticFunction(double capacity = 400, double rate = 4, double center = 5, double minX = 0, double maxX = 10, int numPoints = 100)
    {
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate x-value for each point
            double x = minX + (maxX - minX) * i / (numPoints - 1);
            // Calculate y-value using the logistic function formula
            yValues[i] = capacity / (1 + Math.Exp(-rate * (x - center)));
        }
        return yValues;
    }

    /// <summary>
    /// Produces petal-like shapes.
    /// The <paramref name="petals"/> parameter determines the number of petals in the rose.
    /// If <paramref name="petals"/> is odd, the rose will have <paramref name="petals"/> petals. 
    /// If <paramref name="petals"/> is even, the rose will have 2<paramref name="petals"/> petals.
    /// </summary>
    /// <param name="scaling">the scaling factor that controls the size of the rose</param>
    /// <param name="petals">the number of petals in the rose</param>
    /// <param name="numPoints">the number of points to generate for the curve</param>
    /// <returns></returns>
    public static (double[] xValues, double[] yValues) GenerateRoseCurve(double scaling = 200, int petals = 3, int numPoints = 100)
    {
        double[] xValues = new double[numPoints];
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate angle (theta) for each point
            double theta = Extensions.Tau * i / numPoints;
            // Calculate radius (r) using the rose curve formula
            double r = scaling * Math.Cos(petals * theta);
            // Convert polar coordinates (r, theta) to Cartesian coordinates (x, y)
            xValues[i] = r * Math.Cos(theta);
            yValues[i] = r * Math.Sin(theta);
        }
        return (xValues, yValues);
    }

    /// <summary>
    /// The ratio of frequencies a/b determines the complexity of the Lissajous curve.
    /// The phase shift delta influences the starting point and orientation of the curve.
    /// </summary>
    /// <param name="A">amplitude of the x-component</param>
    /// <param name="B">amplitude of the y-component</param>
    /// <param name="a">frequency of the x-component</param>
    /// <param name="b">frequency of the y-component</param>
    /// <param name="delta">phase shift of the x-component</param>
    /// <param name="numPoints">the number of points to generate for the curve</param>
    /// <returns></returns>
    public static (double[] xValues, double[] yValues) GenerateLissajousCurve(double A = 200, double B = 80, double a = 2, double b = 3, double delta = 0.85, int numPoints = 100)
    {
        delta += Random.Shared.NextDouble(); // Randomize the phase shift a bit.

        double[] xValues = new double[numPoints];
        double[] yValues = new double[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            // Calculate the parameter t (time) for each point
            double t = Extensions.Tau * i / numPoints;
            // Calculate x and y values using the Lissajous equations
            xValues[i] = A * Math.Sin(a * t + delta);
            yValues[i] = B * Math.Sin(b * t);
        }
        return (xValues, yValues);
    }
}
