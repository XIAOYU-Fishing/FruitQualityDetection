import os
import shutil
import random
from pathlib import Path

def prepare_dataset(raw_dir="dataset/raw_images", output_dir="dataset", split=(0.8, 0.1, 0.1)):
    raw_path = Path(raw_dir)
    out_path = Path(output_dir)
    
    for split_name in ["train", "val", "test"]:
        (out_path / "images" / split_name).mkdir(parents=True, exist_ok=True)
        (out_path / "labels" / split_name).mkdir(parents=True, exist_ok=True)
    
    img_exts = ('.jpg', '.jpeg', '.png', '.bmp')
    images = [f for f in raw_path.iterdir() if f.suffix.lower() in img_exts]
    random.seed(42)
    random.shuffle(images)
    
    n = len(images)
    n_train = int(n * split[0])
    n_val = int(n * split[1])
    
    splits = {
        "train": images[:n_train],
        "val": images[n_train:n_train + n_val],
        "test": images[n_train + n_val:]
    }
    
    for split_name, imgs in splits.items():
        for img in imgs:
            shutil.copy(img, out_path / "images" / split_name / img.name)
            label = raw_path / (img.stem + ".txt")
            if label.exists():
                shutil.copy(label, out_path / "labels" / split_name / label.name)
            else:
                print(f"⚠️ 警告: {img.name} 缺少标签文件!")
    
    print(f"✅ 划分完成: 训练集 {len(splits['train'])}, 验证集 {len(splits['val'])}, 测试集 {len(splits['test'])}")

if __name__ == "__main__":
    prepare_dataset()
