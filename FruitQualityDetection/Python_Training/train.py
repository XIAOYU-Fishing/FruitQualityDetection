from ultralytics import YOLO

def main():
    model = YOLO("yolo11n.pt")
    
    model.train(
        data="data.yaml",
        epochs=50,
        imgsz=640,
        batch=4,
        device=0,
        workers=2,
        amp=True,
        patience=10,
        project="runs",
        name="fruit_train",
        exist_ok=True
    )
    
    print("\n========== 测试集评估 ==========")
    metrics = model.val(split='test')
    print(f"mAP50: {metrics.box.map50:.4f}")
    print(f"mAP50-95: {metrics.box.map:.4f}")

if __name__ == "__main__":
    main()