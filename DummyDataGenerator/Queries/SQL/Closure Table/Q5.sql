SELECT DISTINCT o.name, CASE WHEN o.number_of_employees > 5000 THEN "Large" WHEN o.number_of_employees < 1000 THEN "Small" ELSE "Regular" END AS business_type
FROM product AS p
JOIN consists_of AS c ON p.id = c.child_product_id
JOIN supplies AS s ON c.child_product_id = s.product_id
JOIN organization AS o ON s.organization_id = o.id
WHERE c.parent_product_id IN (
    SELECT p2.id
    FROM product AS p2
    JOIN supplies AS s2 ON p2.id = s2.product_id
    JOIN organization AS o2 ON o2.id = s2.organization_id
    WHERE o2.name = 'Top Level Organization Blanda - Bode'
)