using Microsoft.ML.Data;

namespace WPF.BazhenovAI
{

    public class AnomalyPrediction
    {
        // Вектор из 3-х элементов:
        // Prediction[0]: Alert (1 – аномалия, 0 – нормально)
        // Prediction[1]: Аномалия (score)
        // Prediction[2]: P-value
        [VectorType(3)]
        public double[] Prediction { get; set; }

    }

}
