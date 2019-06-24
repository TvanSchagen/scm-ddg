SELECT *
FROM consists_of
JOIN product ON consists_of.child_product_id = product.id
JOIN supplies ON product.id = supplies.product_id
JOIN activity ON supplies.activity_id = activity.id
JOIN organization ON supplies.organization_id = organization.id
WHERE organization.name = "Reilly - Toy"
AND activity.name = "hacking"

AND consists_of.child_product_id IN (SELECT child_product_id
									FROM consists_of
									WHERE parent_product_id = product.id)
                                    
AND consists_of.parent_product_id IN (SELECT parent_product_id
									FROM consists_of
									WHERE child_product_id = product.id
									AND parent_product_id != child_product_id);
                                    
-- we have to recalculate the path_lengths for all the trees that now have become orphaned from the top level product/organization
