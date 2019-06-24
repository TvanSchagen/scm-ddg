DELETE consists_of
FROM consists_of
JOIN product ON consists_of.child_product_id = product.id
JOIN supplies ON product.id = supplies.product_id
JOIN activity ON supplies.activity_id = activity.id
JOIN organization ON supplies.organization_id = organization.id
WHERE organization.name = "Reilly - Toy"
AND activity.name = "hacking"