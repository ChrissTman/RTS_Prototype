using System;
public interface IInvertable
{
    /// <summary>
    /// Every formation defines it's own rules about inverting along Y axis.
    /// </summary>
    /// <param name="angle">Average angle to the formation. Defines how are the units situated to the formation.</param>
    /// <param name="treshold">Threshold in angles</param>
    /// <returns></returns>
    bool CanInvertY(float angle, float treshold);

    /// <summary>
    /// Every formation defines it's own rules about inverting along X axis.
    /// </summary>
    /// <param name="angle"></param>
    /// <param name="treshold"></param>
    /// <returns></returns>
    bool CanInvertX(float angle, float treshold);
}
