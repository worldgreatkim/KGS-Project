using UnityEditor;
using UnityEngine;

/// 생성형 이미지 임포트 유틸: 흰 배경 제거(테두리 플러드필 — 내부 크림색 보존) + 여백 트림 + Sprite 설정
public static class SKImgTool
{
    public const string UI_DIR = "Assets/Package04_Free_Kitchen/_SafeKitchen/Resources/UI";

    /// srcPath의 PNG를 처리해 Resources/UI/{outName}.png로 저장하고 Sprite로 임포트
    public static string Import(string srcPath, string outName, int maxSize)
    {
        if (!System.IO.File.Exists(srcPath)) return "없음: " + srcPath;
        var bytes = System.IO.File.ReadAllBytes(srcPath);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(bytes)) return "디코드 실패: " + srcPath;
        int w = tex.width, h = tex.height;
        var px = tex.GetPixels32();

        // 1) 테두리에서 시작하는 근백색 영역만 배경으로 마킹 (BFS)
        //    배지 안쪽 크림색·하이라이트는 테두리와 안 이어져 있어 보존된다
        var bg = new bool[w * h];
        var queue = new System.Collections.Generic.Queue<int>();
        for (int x = 0; x < w; x++) { TrySeed(px, bg, queue, x); TrySeed(px, bg, queue, (h - 1) * w + x); }
        for (int y = 0; y < h; y++) { TrySeed(px, bg, queue, y * w); TrySeed(px, bg, queue, y * w + w - 1); }
        while (queue.Count > 0)
        {
            int i = queue.Dequeue();
            int cx = i % w, cy = i / w;
            if (cx > 0) TryFill(px, bg, queue, i - 1);
            if (cx < w - 1) TryFill(px, bg, queue, i + 1);
            if (cy > 0) TryFill(px, bg, queue, i - w);
            if (cy < h - 1) TryFill(px, bg, queue, i + w);
        }

        // 2) 배경 → 투명, 경계 1px 페더
        for (int i = 0; i < px.Length; i++) if (bg[i]) px[i] = new Color32(255, 255, 255, 0);
        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                int i = y * w + x;
                if (bg[i] || px[i].a == 0) continue;
                if (bg[i - 1] || bg[i + 1] || bg[i - w] || bg[i + w])
                { var c = px[i]; c.a = 170; px[i] = c; }
            }

        // 3) 불투명 영역 바운딩 박스로 트림 (여백 8px)
        int minX = w, minY = h, maxX = -1, maxY = -1;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                if (px[y * w + x].a > 10)
                {
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
        if (maxX < 0) return "전부 투명이 됨(임계 확인): " + outName;
        minX = Mathf.Max(0, minX - 8); minY = Mathf.Max(0, minY - 8);
        maxX = Mathf.Min(w - 1, maxX + 8); maxY = Mathf.Min(h - 1, maxY + 8);
        int tw = maxX - minX + 1, th = maxY - minY + 1;
        var outPx = new Color32[tw * th];
        for (int y = 0; y < th; y++)
            for (int x = 0; x < tw; x++)
                outPx[y * tw + x] = px[(minY + y) * w + minX + x];
        var outTex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        outTex.SetPixels32(outPx);
        outTex.Apply();

        System.IO.Directory.CreateDirectory(UI_DIR);
        string path = UI_DIR + "/" + outName + ".png";
        System.IO.File.WriteAllBytes(path, outTex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(outTex);
        AssetDatabase.ImportAsset(path);
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.maxTextureSize = maxSize;
            imp.SaveAndReimport();
        }
        return "OK " + outName + " " + tw + "x" + th + " (원본 " + w + "x" + h + ")";
    }

    static void TrySeed(Color32[] px, bool[] bg, System.Collections.Generic.Queue<int> q, int i)
    {
        if (!bg[i] && IsWhite(px[i])) { bg[i] = true; q.Enqueue(i); }
    }

    static void TryFill(Color32[] px, bool[] bg, System.Collections.Generic.Queue<int> q, int i)
    {
        if (!bg[i] && IsWhite(px[i])) { bg[i] = true; q.Enqueue(i); }
    }

    static bool IsWhite(Color32 c)
    {
        return c.r >= 236 && c.g >= 236 && c.b >= 236;
    }
}
