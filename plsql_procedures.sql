
CREATE OR REPLACE PROCEDURE AddProduct(
    p_FarmerID IN NUMBER,
    p_Name IN VARCHAR2,
    p_Category IN VARCHAR2,
    p_Price IN NUMBER,
    p_Description IN VARCHAR2,
    p_StockQuantity IN NUMBER,
    p_ProductID OUT NUMBER
) AS
BEGIN
    INSERT INTO Products (FarmerID, Name, Category, Price, Description, StockQuantity)
    VALUES (p_FarmerID, p_Name, p_Category, p_Price, p_Description, p_StockQuantity)
    RETURNING ProductID INTO p_ProductID;
    
    COMMIT;
END AddProduct;
/


CREATE OR REPLACE PROCEDURE GetProductsByCategory(
    p_Category IN VARCHAR2,
    p_Results OUT SYS_REFCURSOR
) AS
BEGIN
    OPEN p_Results FOR
    SELECT p.*, f.FarmName, f.ContactPerson
    FROM Products p
    JOIN Farmers f ON p.FarmerID = f.FarmerID
    WHERE p.Category = p_Category
    ORDER BY p.Name;
END GetProductsByCategory;
/


CREATE OR REPLACE PROCEDURE PlaceOrder(
    p_CustomerID IN NUMBER,
    p_ProductIDs IN VARCHAR2, -- Comma-separated list
    p_Quantities IN VARCHAR2, -- Comma-separated list
    p_OrderID OUT NUMBER,
    p_TotalAmount OUT NUMBER
) AS
    v_ProductIDArray APEX_APPLICATION_GLOBAL.VC_ARR2;
    v_QuantityArray APEX_APPLICATION_GLOBAL.VC_ARR2;
    v_Price NUMBER;
    v_ItemTotal NUMBER;
BEGIN
    -- Initialize order
    INSERT INTO Orders (CustomerID, TotalAmount)
    VALUES (p_CustomerID, 0)
    RETURNING OrderID, TotalAmount INTO p_OrderID, p_TotalAmount;
    
    -- Split input strings
    v_ProductIDArray := APEX_UTIL.STRING_TO_TABLE(p_ProductIDs);
    v_QuantityArray := APEX_UTIL.STRING_TO_TABLE(p_Quantities);
    
    -- Process each item
    FOR i IN 1..v_ProductIDArray.COUNT LOOP
        -- Get product price
        SELECT Price INTO v_Price FROM Products WHERE ProductID = TO_NUMBER(v_ProductIDArray(i));
        
        -- Calculate item total
        v_ItemTotal := v_Price * TO_NUMBER(v_QuantityArray(i));
        
        -- Add order item
        INSERT INTO OrderItems (OrderID, ProductID, Quantity, UnitPrice)
        VALUES (p_OrderID, TO_NUMBER(v_ProductIDArray(i)), TO_NUMBER(v_QuantityArray(i)), v_Price);
        
       
        UPDATE Products 
        SET StockQuantity = StockQuantity - TO_NUMBER(v_QuantityArray(i))
        WHERE ProductID = TO_NUMBER(v_ProductIDArray(i));
        
        -- Accumulate total
        p_TotalAmount := p_TotalAmount + v_ItemTotal;
    END LOOP;
    
    
    UPDATE Orders SET TotalAmount = p_TotalAmount WHERE OrderID = p_OrderID;
    
    COMMIT;
END PlaceOrder;
/
