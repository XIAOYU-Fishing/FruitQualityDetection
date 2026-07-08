from ultralytics import YOLO

model = YOLO("runs/fruit_train/weights/best.pt")

model.export(
    format="onnx",
    imgsz=640,
    dynamic=False,
    nms=False,
    simplify=True,
    opset=12,
    batch=1
)

print("✅ 导出完成: runs/fruit_train/weights/best.onnx")
print("请复制到 C# 项目的 models/ 目录")