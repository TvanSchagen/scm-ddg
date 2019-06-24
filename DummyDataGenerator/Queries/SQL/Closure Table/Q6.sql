SELECT p.id, p.name
FROM product AS p
JOIN consists_of AS c ON p.id = c.child_product_id
JOIN supplies AS s ON c.child_product_id = s.product_id
JOIN organization AS o ON s.organization_id = o.id
WHERE c.parent_product_id IN (
    SELECT p2.id
    FROM product AS p2
    JOIN consists_of AS c2 ON p2.id = c2.child_product_id
    JOIN supplies AS s2 ON c2.child_product_id = s2.product_id
	JOIN organization AS o2 ON s2.organization_id = o2.id
    JOIN location AS l2 ON s2.location_id = l2.id
    WHERE p2.category = "Baby"
	AND p2.created > '2017-01-01 00:00:00'
	AND o2.number_of_employees > 1000
	AND l2.country IN ("Guam", "Georgia", "Dominica")
)
AND c.path_length < 4