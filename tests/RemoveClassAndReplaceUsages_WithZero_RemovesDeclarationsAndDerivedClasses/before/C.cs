class Base {}
class Derived : Base {}
class C {}
class D {
    C c;
    Base b;
    void M() {
        C local = null;
    }
}