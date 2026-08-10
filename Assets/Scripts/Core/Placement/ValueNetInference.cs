using Unity.Sentis;
using UnityEngine;

/// <summary>
/// Static utility wrapping Sentis inference for the value network.
/// Thread-unsafe — call from main thread only (Sentis tensors are not thread-safe).
/// </summary>
public static class ValueNetInference
{
    private static Worker worker;

    public static bool IsLoaded => worker != null;

    /// <summary>Load an ONNX model imported as a Sentis ModelAsset.</summary>
    public static void LoadModel(ModelAsset modelAsset)
    {
        Dispose();
        var runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);
    }

    /// <summary>Evaluate a single board and return the scalar value estimate.</summary>
    public static float Evaluate(byte[] boardOccupancy, int width, int height)
    {
        int boardSize = width * height;
        float[] data = new float[boardSize];
        for (int i = 0; i < boardSize; i++)
            data[i] = boardOccupancy[i];

        using var input = new Tensor<float>(new TensorShape(1, 1, height, width), data);
        worker.Schedule(input);
        var output = worker.PeekOutput() as Tensor<float>;
        var values = output.DownloadToArray();
        return values[0];
    }

    /// <summary>
    /// Evaluate a batch of boards in a single forward pass.
    /// boards[i] is a byte[width*height] occupancy grid; results[i] receives the value.
    /// </summary>
    public static void EvaluateBatch(byte[][] boards, float[] results, int count, int width, int height)
    {
        int boardSize = width * height;
        float[] data = new float[count * boardSize];
        for (int i = 0; i < count; i++)
        {
            int offset = i * boardSize;
            for (int j = 0; j < boardSize; j++)
                data[offset + j] = boards[i][j];
        }

        using var input = new Tensor<float>(new TensorShape(count, 1, height, width), data);
        worker.Schedule(input);
        var output = worker.PeekOutput() as Tensor<float>;
        var values = output.DownloadToArray();
        for (int i = 0; i < count; i++)
            results[i] = values[i];
    }

    public static void Dispose()
    {
        worker?.Dispose();
        worker = null;
    }
}
