-- modify this such that it can return an actual hierarchy, not just the list of products

SELECT p.id, p.name
FROM product AS p
JOIN consists_of AS c ON p.id = c.child_product_id
JOIN supplies AS s ON c.child_product_id = s.product_id
JOIN organization AS o ON s.organization_id = o.id
WHERE c.parent_product_id IN (
    SELECT p2.id
    FROM product AS p2
    WHERE p2.name = 'Top Level Product Awesome Steel Car'
)
AND c.path_length < 5