DELETE consists_of
FROM consists_of
JOIN product ON consists_of.child_product_id = product.id
JOIN supplies ON product.id = supplies.product_id
JOIN activity ON supplies.activity_id = activity.id
JOIN organization ON supplies.organization_id = organization.id
WHERE organization.name = "Reilly - Toy"
AND activity.name = "hacking"
AND consists_of.child_product_id IN (SELECT descendant
									FROM TreePaths
									WHERE ancestor = product.id)
AND consists_of.parent_product_id IN (SELECT ancestor
									FROM TreePaths
									WHERE descendant = product.id
									AND ancestor != descendant);

-- insert stuff

INSERT INTO TreePaths (ancestor, descendant)
	SELECT supertree.ancestor, subtree.descendant
	FROM TreePaths AS supertree
		CROSS JOIN TreePaths AS subtree
	WHERE supertree.descendant = 3
		AND subtree.ancestor = 6;