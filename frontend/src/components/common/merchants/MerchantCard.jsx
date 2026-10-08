import { Thumbnail } from "../Thumbnail.jsx";
import { Wrench, Store, Star, MapPin, ArrowRight } from "lucide-react";
import { number, money } from "../../../shared/utils/format.js";

export function MerchantCard({ item, type, mechanic }) {
  return (
    <article className="card merchant-card" key={item.id}>
      <a href={`#/${type}/${item.slug}`}>
        <Thumbnail
          src={item.imageUrl}
          icon={mechanic ? Wrench : Store}
          alt={item.businessName || item.storeName}
        />
      </a>
      <div className="merchant-body">
        <div className="merchant-rating">
          <Star size={13} fill="currentColor" /> {number(item.rating)}{" "}
          <span className="meta">/ 5</span>
        </div>
        <h2>
          <a href={`#/${type}/${item.slug}`}>
            {item.businessName || item.storeName}
          </a>
        </h2>
        <p className="muted line-clamp">{item.description}</p>
        <p className="meta address">
          <MapPin size={14} />
          {item.address}
        </p>
        <div className="card-footer">
          {mechanic && item.hourlyRate != null ? (
            <strong>
              {money(item.hourlyRate)} <small className="meta">/ saat</small>
            </strong>
          ) : (
            <span className="meta">
              {mechanic ? "Qiymət razılaşma ilə" : "Mağaza profili"}
            </span>
          )}
          <a className="text-link" href={`#/${type}/${item.slug}`}>
            Ətraflı <ArrowRight size={14} />
          </a>
        </div>
      </div>
    </article>
  );
}
