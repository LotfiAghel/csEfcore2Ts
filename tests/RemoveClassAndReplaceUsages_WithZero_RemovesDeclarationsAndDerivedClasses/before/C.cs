class Base {}
class Derived : Base {}
class C {}
class D {
    C c;
    Base b;
    Base tt{ get; set; }
    void M() {
        C local = null;
    }
}