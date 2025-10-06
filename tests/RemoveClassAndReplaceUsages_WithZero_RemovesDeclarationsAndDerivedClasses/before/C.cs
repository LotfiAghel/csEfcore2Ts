class Base {}
class Derived : Base {}
class C {}
class D {
    C c;
    Base b;
    Base tt { get; set; }
    Derived dd { get; set; }
    void M() {
        C local = null;
    }
}