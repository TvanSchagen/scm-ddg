SELECT product.id AS q7, product.name, product.ean, product.description, organization.name, organization.description
FROM product
JOIN supplies ON product.id = supplies.product_id
JOIN organization ON organization_id = supplies.organization_id
WHERE product.description LIKE "%corporis dolor%"