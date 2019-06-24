SET @input_product_id = 102;
SET @new_product_id = 999;

-- insert the new relation to the parent
INSERT INTO consists_of VALUES
	(@input_product_id, @newid);
    
-- insert the new relations to the children
INSERT INTO consists_of(parent_id, child_id)
SELECT @newid, product.id
	FROM consists_of
	JOIN product ON consists_of.child_product_id = product.id
	WHERE product.id = @input_product_id;

-- delete the original relation
DELETE consists_of
FROM consists_of
JOIN product ON consists_of.child_product_id = product.id
WHERE product.id = @input_product_id
-- make sure to not delete our new relations
AND product.id NOT IN (
	SELECT product.id 
    FROM product
    WHERE product.id = @newid
);