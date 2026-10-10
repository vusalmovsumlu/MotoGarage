import { useApp } from "../../../shared/hooks/useApp.js";

import { Thumbnail } from "../../../components/common/Thumbnail.jsx";

import { Package, Plus } from "lucide-react";

import { money, number } from "../../../shared/utils/format.js";

import { Button } from "../../../components/ui/Button.jsx";

export function ProductCard({ product, readOnly = false }) {
  const { addCart } = useApp();
  const Image = readOnly ? "div" : "a";
  return (
    <article className="card product-card">
      <Image
        {...(!readOnly && { href: `#/products/${product.slug}` })}
        className="product-image"
      >
        <Thumbnail src={product.imageUrl} icon={Package} alt={product.name} />
      </Image>
      <div className="product-body">
        <span className="meta">{product.brand || "Ehtiyat hissəsi"}</span>
        <h2>
          {readOnly ? (
            product.name
          ) : (
            <a href={`#/products/${product.slug}`}>{product.name}</a>
          )}
        </h2>
        <p className="meta">
          {product.vehicleCompatibility ||
            "Uyğunluğu satıcı ilə dəqiqləşdirin."}
        </p>
        <div className="product-bottom">
          <strong>{money(product.price)}</strong>
          <span className={`stock-label ${!product.stock ? "sold-out" : ""}`}>
            {product.stock > 0
              ? `${number(product.stock)} ədəd stokda`
              : "Stokda yoxdur"}
          </span>
        </div>
        {!readOnly && (
          <Button
            variant="secondary"
            disabled={product.stock < 1}
            onClick={() => addCart(product)}
          >
            <Plus size={15} /> Səbətə əlavə et
          </Button>
        )}
      </div>
    </article>
  );
}
