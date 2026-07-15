using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace Infrastructure.Services;

public class TextEmbeddingService : ITextEmbeddingService, IDisposable
{
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer;

    public TextEmbeddingService(string modelPath, string vocabPath)
    {
        if (!File.Exists(modelPath) || !File.Exists(vocabPath))
            throw new FileNotFoundException("ONNX model or vocab not found.");

        _session = new InferenceSession(modelPath);
        
        using var stream = File.OpenRead(vocabPath);
        _tokenizer = WordPieceTokenizer.Create(stream);
    }

    public Task<float[]> GenerateEmbeddingAsync(string text)
    {
        var idsList = _tokenizer.EncodeToIds(text).ToList();
        // Add CLS and SEP tokens if not present
        if (idsList.Count == 0 || idsList[0] != 101) idsList.Insert(0, 101);
        if (idsList.Last() != 102) idsList.Add(102);
        
        var inputIds = idsList.Select(x => (long)x).ToArray();
        var attentionMask = Enumerable.Repeat(1L, inputIds.Length).ToArray();
        var tokenTypeIds = Enumerable.Repeat(0L, inputIds.Length).ToArray();

        var inputIdsTensor = new DenseTensor<long>(inputIds, new[] { 1, inputIds.Length });
        var attentionMaskTensor = new DenseTensor<long>(attentionMask, new[] { 1, inputIds.Length });
        var tokenTypeIdsTensor = new DenseTensor<long>(tokenTypeIds, new[] { 1, inputIds.Length });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        using var results = _session.Run(inputs);
        
        var outputTensor = results.First().AsTensor<float>();
        
        var dimensions = outputTensor.Dimensions;
        int seqLen = dimensions[1];
        int hiddenSize = dimensions[2];

        var pooled = new float[hiddenSize];
        for (int i = 0; i < seqLen; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                pooled[j] += outputTensor[0, i, j];
            }
        }
        for (int j = 0; j < hiddenSize; j++)
        {
            pooled[j] /= seqLen;
        }

        double sumSq = pooled.Sum(x => x * x);
        float norm = (float)Math.Sqrt(sumSq);
        for (int j = 0; j < hiddenSize; j++)
        {
            pooled[j] /= norm;
        }

        return Task.FromResult(pooled);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
