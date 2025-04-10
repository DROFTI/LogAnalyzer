using Microsoft.ML;

namespace WPF.BazhenovAI
{

    public class AnomalyDetector
    {
        private readonly MLContext _mlContext;
        private ITransformer _model;
        private readonly int _trainingWindowSize;
        private readonly int _seasonalityWindowSize;

        public AnomalyDetector(IEnumerable<LogData> logs, int trainingWindowSize = 30, int seasonalityWindowSize = 7, double sensitivity = 95.0, double confidence = 95.0)
        {
            _mlContext = new MLContext();

            var data = logs.ToList();
            IDataView trainingData = _mlContext.Data.LoadFromEnumerable(data);

            _trainingWindowSize = trainingWindowSize;
            _seasonalityWindowSize = seasonalityWindowSize;

            var pipeline = _mlContext.Transforms.DetectChangePointBySsa(
                outputColumnName: "Prediction",
                inputColumnName: nameof(LogData.ErrorCount),
                trainingWindowSize: _trainingWindowSize + 1,
                seasonalityWindowSize: _seasonalityWindowSize,
                confidence: confidence,
                changeHistoryLength: _trainingWindowSize);
                

            _model = pipeline.Fit(trainingData);
        }

        /// <summary>
        /// Выполняет предсказание для входных данных и возвращает коллекцию,
        /// где для каждого элемента указывается, является ли он аномалией и его score.
        /// </summary>
        public IEnumerable<(LogData log, bool isAnomaly, double score)> Predict(IEnumerable<LogData> logs)
        {
            IDataView data = _mlContext.Data.LoadFromEnumerable(logs);
            IDataView predictions = _model.Transform(data);

            var predictionResults = _mlContext.Data.CreateEnumerable<AnomalyPrediction>(predictions, reuseRowObject: false).ToList();
            var logsList = logs.ToList();

            for (int i = 0; i < predictionResults.Count && i < logsList.Count; i++)
            {
                
                var prediction = predictionResults[i].Prediction;
                bool isAnomaly = Math.Abs(prediction[0]) < 1e-6;
                double score = prediction[1];
                yield return (logsList[i], isAnomaly, score);
            }
        }
    }


}
